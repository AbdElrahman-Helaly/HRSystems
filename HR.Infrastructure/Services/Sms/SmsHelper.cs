using System.Text;
using System.Text.Json;

namespace internalEmployee.Services.Sms;

public static class SmsHelper
{
    private static readonly HttpClient HttpClient = new();

    private const string DefaultApiUrl = "https://bulk.whysms.com/api/http/sms/send";
    private const string DefaultSenderId = "Mediconsult";

    public static void SendSms(string recipient, string message, string? senderId = null, string? apiToken = null, string? apiUrl = null, CancellationToken ct = default)
    {
        _ = SendSmsAsync(recipient, message, senderId, apiToken, apiUrl, ct);
    }

    private static string FormatPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return phoneNumber;

        var formatted = phoneNumber.Replace(" ", string.Empty, StringComparison.Ordinal);

        if (!formatted.StartsWith("0", StringComparison.Ordinal))
            formatted = "0" + formatted;

        if (formatted.Length == 11)
            formatted = "2" + formatted;

        return formatted;
    }

    public static async Task<bool> SendSmsAsync(string recipient, string message, string? senderId = null, string? apiToken = null, string? apiUrl = null, CancellationToken ct = default)
    {
        var result = await SendSmsWithErrorAsync(recipient, message, senderId, apiToken, apiUrl, ct).ConfigureAwait(false);
        return result.Success;
    }

    public static async Task<SmsSendResult> SendSmsWithErrorAsync(string recipient, string message, string? senderId = null, string? apiToken = null, string? apiUrl = null, CancellationToken ct = default)
    {
        try
        {
            var formattedRecipient = FormatPhoneNumber(recipient);
            var requestBody = new
            {
                api_token = apiToken ?? string.Empty,
                recipient = formattedRecipient,
                sender_id = senderId ?? DefaultSenderId,
                type = "plain",
                message = message
            };

            var jsonBody = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl ?? DefaultApiUrl)
            {
                Content = content
            };
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await HttpClient.SendAsync(request, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
                return new SmsSendResult { Success = true, StatusCode = (int)response.StatusCode };

            var responseContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var error = $"SMS send failed - Status: {(int)response.StatusCode} {response.StatusCode}, Response: {responseContent}";
            System.Diagnostics.Debug.WriteLine($"SmsHelper: {error}");
            return new SmsSendResult { Success = false, StatusCode = (int)response.StatusCode, Error = error };
        }
        catch (Exception ex)
        {
            var error = $"Error sending SMS - {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"SmsHelper: {error}\nStack Trace: {ex.StackTrace}");
            return new SmsSendResult { Success = false, Error = error };
        }
    }
}

public sealed class SmsSendResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public int? StatusCode { get; init; }
}
