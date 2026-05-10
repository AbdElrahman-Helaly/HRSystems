namespace internalEmployee.Services.Notification;

public interface IFcmService
{
    Task<bool> SendPushNotificationAsync(string token, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default);
    Task SendPushNotificationToMultipleAsync(List<string> tokens, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default);
}

