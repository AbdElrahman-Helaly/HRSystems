using internalEmployee.Auth.Contracts;
using internalEmployee.Data.Entities;
using NotificationEntity = internalEmployee.Data.Entities.Notification;

namespace internalEmployee.Services.Notification;

public interface INotificationService
{
    Task SendRequestNotificationAsync(Guid requestUserId, NotificationType type, int requestId, string requestMessage, CancellationToken ct = default);
    Task SendRequestReminderAsync(Guid requestUserId, NotificationType type, int requestId, string requestMessage, CancellationToken ct = default);
    Task SendStatusChangeNotificationAsync(Guid requestUserId, NotificationType type, int requestId, RequestStatus newStatus, string? rejectionReason, CancellationToken ct = default);
    Task<List<NotificationEntity>> GetUserNotificationsAsync(Guid userId, CancellationToken ct = default);
    Task MarkNotificationAsReadAsync(int notificationId, Guid userId, CancellationToken ct = default);
    Task ConfirmNotificationAsync(int notificationId, Guid userId, CancellationToken ct = default);
    Task MarkAllNotificationsAsReadAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task RegisterFcmTokenAsync(Guid userId, string token, string? deviceInfo, CancellationToken ct = default);
    Task UnregisterFcmTokenAsync(Guid userId, string token, CancellationToken ct = default);
    Task SendMeetingNotificationAsync(int meetingId, string title, string message, List<Guid> recipientUserIds, CancellationToken ct = default);
    Task<List<NotificationUserLookupResponse>> SearchUsersForDirectNotificationAsync(
        Guid senderUserId,
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default);
    Task SendDirectNotificationAsync(Guid senderUserId, Guid recipientUserId, string title, string message, CancellationToken ct = default);
    Task SendCustomNotificationAsync(
        NotificationType type,
        int requestId,
        string title,
        string message,
        IReadOnlyCollection<Guid> recipientUserIds,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default);
    Task SendBroadcastNotificationAsync(Guid senderUserId, BroadcastNotificationRequest request, CancellationToken ct = default);
}

