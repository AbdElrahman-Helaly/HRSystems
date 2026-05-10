using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;


namespace internalEmployee.Services.Notification;

public sealed class FcmService : IFcmService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FcmService> _logger;
    private static bool _firebaseInitialized = false;
    private static readonly object _lock = new object();


private readonly IWebHostEnvironment _env;

public FcmService(
    IConfiguration configuration,
    ILogger<FcmService> logger,
    IWebHostEnvironment env)
{
    _configuration = configuration;
    _logger = logger;
    _env = env;

    InitializeFirebase();
}


    private void InitializeFirebase()
    {
        if (_firebaseInitialized)
            return;

        lock (_lock)
        {
            if (_firebaseInitialized)
                return;

            try
            {
                var serviceAccountPath = _configuration["Firebase:ServiceAccountPath"];
                if (string.IsNullOrWhiteSpace(serviceAccountPath))
                {
                    _logger.LogWarning("Firebase ServiceAccountPath is not configured. FCM push notifications will be disabled.");
                    return;
                }

                var fullPath = Path.Combine(_env.ContentRootPath, serviceAccountPath);
                if (!File.Exists(fullPath))
                {
                    _logger.LogWarning($"Firebase service account file not found at: {fullPath}. FCM push notifications will be disabled.");
                    return;
                }

                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile(fullPath)
                });

                _firebaseInitialized = true;
                _logger.LogInformation("Firebase initialized successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firebase. FCM push notifications will be disabled.");
            }
        }
    }

    public async Task<bool> SendPushNotificationAsync(string token, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        if (!_firebaseInitialized)
        {
            _logger.LogWarning("Firebase not initialized. Cannot send push notification.");
            return false;
        }

        try
        {
            var message = new Message
            {
                Token = token,
                Notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = title,
                    Body = body
                },
                Data = data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>()
            };

            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message, ct);
            _logger.LogInformation($"Successfully sent message to token: {token}, Response: {response}");
            return true;
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogWarning(ex, $"Failed to send FCM message to token: {token}. Error code: {ex.ErrorCode}");
            // Handle invalid tokens - could be expired or unregistered
            if (ex.ErrorCode == ErrorCode.InvalidArgument || ex.ErrorCode == ErrorCode.NotFound)
            {
                // Token is invalid, should be removed from database
                return false;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error sending FCM message to token: {token}");
            return false;
        }
    }

    public async Task SendPushNotificationToMultipleAsync(List<string> tokens, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        if (!_firebaseInitialized)
        {
            _logger.LogWarning("Firebase not initialized. Cannot send push notifications.");
            return;
        }

        if (tokens == null || tokens.Count == 0)
        {
            _logger.LogWarning("No tokens provided. Cannot send push notifications.");
            return;
        }

        _logger.LogInformation($"Attempting to send FCM notification to {tokens.Count} token(s). Title: {title}, Body: {body}");

        int successCount = 0;
        int failureCount = 0;

        // Send to each token individually (more reliable than multicast)
        foreach (var token in tokens)
        {
            try
            {
                var success = await SendPushNotificationAsync(token, title, body, data, ct);
                if (success)
                    successCount++;
                else
                    failureCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to send FCM message to token {token.Substring(0, Math.Min(20, token.Length))}...");
                failureCount++;
            }
        }

        _logger.LogInformation($"FCM Summary: Successfully sent {successCount} out of {tokens.Count} FCM messages. Failures: {failureCount}");
    }
}

