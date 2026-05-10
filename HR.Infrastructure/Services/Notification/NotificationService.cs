using internalEmployee.Auth.Models;
using internalEmployee.Auth.Contracts;
using internalEmployee.Data;
using internalEmployee.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationEntity = internalEmployee.Data.Entities.Notification;

namespace internalEmployee.Services.Notification;

public sealed class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly IFcmService _fcmService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext db, IFcmService fcmService, ILogger<NotificationService> logger)
    {
        _db = db;
        _fcmService = fcmService;
        _logger = logger;
    }

    public async Task SendRequestNotificationAsync(Guid requestUserId, NotificationType type, int requestId, string requestMessage, CancellationToken ct = default)
    {
        await SendRequestNotificationCoreAsync(
            requestUserId,
            type,
            requestId,
            requestMessage,
            "طلب جديد",
            "طلب {0} جديد",
            ct);
    }

    public async Task SendRequestReminderAsync(Guid requestUserId, NotificationType type, int requestId, string requestMessage, CancellationToken ct = default)
    {
        await SendRequestNotificationCoreAsync(
            requestUserId,
            type,
            requestId,
            requestMessage,
            "تذكير",
            "تذكير بطلب {0}",
            ct);
    }

    private async Task SendRequestNotificationCoreAsync(
        Guid requestUserId,
        NotificationType type,
        int requestId,
        string requestMessage,
        string messagePrefix,
        string titleFormat,
        CancellationToken ct)
    {
        // Get the user who created the request
        var requestUser = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == requestUserId, ct);

        if (requestUser == null)
        {
            _logger.LogWarning($"User {requestUserId} not found. Cannot send notifications.");
            return;
        }

        // Find recipients:
        // 1. Department Admin (Admin role, same DepartmentId)
        // 2. All HR users
        // 3. All SuperAdmins

        var recipients = new List<AppUser>();

        // Find department admin
        if (requestUser.DepartmentId.HasValue)
        {
            var departmentAdmins = await _db.Users
                .Where(u => u.DepartmentId == requestUser.DepartmentId.Value && u.Role == AppRole.Admin && u.IsActive)
                .ToListAsync(ct);
            recipients.AddRange(departmentAdmins);
            _logger.LogInformation($"Found {departmentAdmins.Count} department admins for DeptId {requestUser.DepartmentId}");
        }

        // Find all active HR users
        var hrUsers = await _db.Users
            .Where(u => u.Role == AppRole.HR && u.IsActive)
            .ToListAsync(ct);
        recipients.AddRange(hrUsers);
        _logger.LogInformation($"Found {hrUsers.Count} active HR users");

        // Find all active SuperAdmins
        var superAdmins = await _db.Users
            .Where(u => u.Role == AppRole.SuperAdmin && u.IsActive)
            .ToListAsync(ct);
        recipients.AddRange(superAdmins);
        _logger.LogInformation($"Found {superAdmins.Count} active SuperAdmin users");

        // Remove duplicates (in case user is both admin and superadmin)
        recipients = recipients.DistinctBy(u => u.Id).ToList();

        if (recipients.Count == 0)
        {
            _logger.LogWarning($"No active recipients found for {type} notification. Requester: {requestUserId}, DeptId: {requestUser.DepartmentId}");
            return;
        }

        _logger.LogInformation($"Total unique recipients for notification: {recipients.Count}");

        // Create notification type name
        var typeName = type switch
        {
            NotificationType.Permission => "إذن",
            NotificationType.Leave => "إجازة",
            NotificationType.Assignment => "مامورية",
            NotificationType.Overtime => "عمل إضافي",
            NotificationType.SalaryAdvance => "سلفة",
            NotificationType.RecruitmentRequest => "احتياج توظيف",
            NotificationType.RecruitmentCandidate => "مرشح توظيف",
            NotificationType.WorkFromHome => "عمل من المنزل",
            _ => "طلب"
        };

        // Build full name (prefer Arabic, fallback to English, then NationalId/PassportNumber)
        string? userName = null;
        if (!string.IsNullOrWhiteSpace(requestUser.FirstNameAr) || !string.IsNullOrWhiteSpace(requestUser.LastNameAr))
        {
            userName = $"{requestUser.FirstNameAr} {requestUser.MiddleNameAr} {requestUser.LastNameAr}".Trim();
        }
        else if (!string.IsNullOrWhiteSpace(requestUser.FirstNameEn) || !string.IsNullOrWhiteSpace(requestUser.LastNameEn))
        {
            userName = $"{requestUser.FirstNameEn} {requestUser.MiddleNameEn} {requestUser.LastNameEn}".Trim();
        }
        else
        {
            userName = requestUser.NationalId ?? requestUser.PassportNumber ?? "Unknown";
        }

        var message = $"{messagePrefix}: {typeName} من {userName}";
        var title = string.Format(titleFormat, typeName);

        // Create database notifications first
        var notifications = new List<NotificationEntity>();
        foreach (var recipient in recipients)
        {
            var notification = new NotificationEntity
            {
                UserId = recipient.Id,
                Type = type,
                RequestId = requestId,
                Message = $"{message}. {requestMessage}",
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _db.Notifications.Add(notification);
            notifications.Add(notification);
        }

        // Save notifications to get IDs
        await _db.SaveChangesAsync(ct);

        // Now send FCM push notifications
        for (int i = 0; i < recipients.Count; i++)
        {
            var recipient = recipients[i];
            var notification = notifications[i];

            try
            {
                var fcmTokens = await _db.FcmTokens
                    .Where(t => t.UserId == recipient.Id)
                    .Select(t => t.Token)
                    .ToListAsync(ct);

                if (fcmTokens.Count > 0)
                {
                    var data = new Dictionary<string, string>
                    {
                        { "type", type.ToString() },
                        { "requestId", requestId.ToString() },
                        { "id", requestId.ToString() }, // Alias for requestId to support different client implementations
                        { "notificationId", notification.Id.ToString() }
                    };

                    var recipientName = recipient.FirstNameEn ?? recipient.FirstNameAr ?? recipient.NationalId ?? recipient.PassportNumber ?? "Unknown";
                    _logger.LogInformation($"Sending FCM notification to user {recipient.Id} ({recipientName}, Role: {recipient.Role}) with {fcmTokens.Count} token(s)");
                    
                    await _fcmService.SendPushNotificationToMultipleAsync(
                        fcmTokens,
                        title,
                        message,
                        data,
                        ct);

                    // Update LastUsedAt for tokens
                    var tokensToUpdate = await _db.FcmTokens
                        .Where(t => fcmTokens.Contains(t.Token))
                        .ToListAsync(ct);
                    foreach (var token in tokensToUpdate)
                    {
                        token.LastUsedAt = DateTime.Now;
                    }
                    
                    // Save token updates
                    await _db.SaveChangesAsync(ct);
                }
                else
                {
                    var recipientName = recipient.FirstNameEn ?? recipient.FirstNameAr ?? recipient.NationalId ?? recipient.PassportNumber ?? "Unknown";
                    _logger.LogWarning($"User {recipient.Id} ({recipientName}, Role: {recipient.Role}) has no FCM tokens registered. Skipping push notification.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send FCM notification to user {recipient.Id}");
                // Continue even if FCM fails - database notification is still created
            }
        }
    }

    public async Task<List<NotificationEntity>> GetUserNotificationsAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task MarkNotificationAsReadAsync(int notificationId, Guid userId, CancellationToken ct = default)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct);

        if (notification != null)
        {
            notification.IsRead = true;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task ConfirmNotificationAsync(int notificationId, Guid userId, CancellationToken ct = default)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct);

        if (notification != null)
        {
            notification.IsConfirmed = true;
            notification.ConfirmedAt = DateTime.Now;
            notification.IsRead = true; // Confirmation also implies it's read
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task MarkAllNotificationsAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        var notifications = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);
    }

    public async Task RegisterFcmTokenAsync(Guid userId, string token, string? deviceInfo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("FCM token is required.");

        // Check if token already exists
        var existingToken = await _db.FcmTokens
            .FirstOrDefaultAsync(t => t.Token == token, ct);

        if (existingToken != null)
        {
            // Update existing token (user might have logged in on same device)
            existingToken.UserId = userId;
            existingToken.DeviceInfo = deviceInfo;
            existingToken.LastUsedAt = DateTime.Now;
        }
        else
        {
            // Create new token
            var fcmToken = new FcmToken
            {
                UserId = userId,
                Token = token,
                DeviceInfo = deviceInfo,
                CreatedAt = DateTime.Now
            };
            _db.FcmTokens.Add(fcmToken);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task UnregisterFcmTokenAsync(Guid userId, string token, CancellationToken ct = default)
    {
        var fcmToken = await _db.FcmTokens
            .FirstOrDefaultAsync(t => t.Token == token && t.UserId == userId, ct);

        if (fcmToken != null)
        {
            _db.FcmTokens.Remove(fcmToken);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task SendStatusChangeNotificationAsync(Guid requestUserId, NotificationType type, int requestId, RequestStatus newStatus, string? rejectionReason, CancellationToken ct = default)
    {
        // Get the user who created the request
        var requestUser = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == requestUserId, ct);

        if (requestUser == null)
        {
            _logger.LogWarning($"User {requestUserId} not found. Cannot send status change notification.");
            return;
        }

        // Create notification type name
        var typeName = type switch
        {
            NotificationType.Permission => "إذن",
            NotificationType.Leave => "إجازة",
            NotificationType.Assignment => "مامورية",
            NotificationType.Overtime => "عمل إضافي",
            NotificationType.SalaryAdvance => "سلفة",
            NotificationType.RecruitmentRequest => "احتياج توظيف",
            NotificationType.RecruitmentCandidate => "مرشح توظيف",
            NotificationType.WorkFromHome => "عمل من المنزل",
            _ => "طلب"
        };

        // Create status message
        var statusMessage = newStatus switch
        {
            RequestStatus.Approved => "تم الموافقة على",
            RequestStatus.Rejected => "تم رفض",
            _ => "تم تحديث حالة"
        };

        var message = $"{statusMessage} طلب {typeName} الخاص بك";
        if (newStatus == RequestStatus.Rejected && !string.IsNullOrWhiteSpace(rejectionReason))
        {
            message += $". سبب الرفض: {rejectionReason}";
        }

        // Create database notification
        var notification = new NotificationEntity
        {
            UserId = requestUserId,
            Type = type,
            RequestId = requestId,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.Now
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);

        // Send FCM push notification
        try
        {
            var fcmTokens = await _db.FcmTokens
                .Where(t => t.UserId == requestUserId)
                .Select(t => t.Token)
                .ToListAsync(ct);

            if (fcmTokens.Count > 0)
            {
                var data = new Dictionary<string, string>
                {
                    { "type", type.ToString() },
                    { "requestId", requestId.ToString() },
                    { "id", requestId.ToString() }, // Alias for requestId to support different client implementations
                    { "status", newStatus.ToString() },
                    { "notificationId", notification.Id.ToString() }
                };

                _logger.LogInformation($"Sending FCM notification to user {requestUserId} with {fcmTokens.Count} token(s)");
                
                await _fcmService.SendPushNotificationToMultipleAsync(
                    fcmTokens,
                    $"تحديث حالة {typeName}",
                    message,
                    data,
                    ct);

                // Update LastUsedAt for tokens
                var tokensToUpdate = await _db.FcmTokens
                    .Where(t => fcmTokens.Contains(t.Token))
                    .ToListAsync(ct);
                foreach (var token in tokensToUpdate)
                {
                    token.LastUsedAt = DateTime.Now;
                }
                
                await _db.SaveChangesAsync(ct);
            }
            else
            {
                _logger.LogInformation($"User {requestUserId} has no FCM tokens registered. Skipping push notification.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send FCM status change notification to user {requestUserId}");
            // Continue even if FCM fails - database notification is still created
        }
    }
    public async Task SendMeetingNotificationAsync(int meetingId, string title, string message, List<Guid> recipientUserIds, CancellationToken ct = default)
    {
        if (recipientUserIds == null || recipientUserIds.Count == 0)
            return;

        // Create database notifications
        var notifications = new List<NotificationEntity>();
        foreach (var recipientId in recipientUserIds)
        {
            var notification = new NotificationEntity
            {
                UserId = recipientId,
                Type = NotificationType.Meeting,
                RequestId = meetingId,
                Message = $"{title}: {message}",
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _db.Notifications.Add(notification);
            notifications.Add(notification);
        }

        await _db.SaveChangesAsync(ct);

        // Send FCM notifications
        foreach (var recipientId in recipientUserIds)
        {
            try
            {
                var fcmTokens = await _db.FcmTokens
                    .Where(t => t.UserId == recipientId)
                    .Select(t => t.Token)
                    .ToListAsync(ct);

                if (fcmTokens.Count > 0)
                {
                    var data = new Dictionary<string, string>
                    {
                        { "type", NotificationType.Meeting.ToString() },
                        { "meetingId", meetingId.ToString() },
                        { "id", meetingId.ToString() }
                    };

                    await _fcmService.SendPushNotificationToMultipleAsync(
                        fcmTokens,
                        title,
                        message,
                        data,
                        ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send FCM meeting notification to user {recipientId}");
            }
        }
    }

    public async Task<List<NotificationUserLookupResponse>> SearchUsersForDirectNotificationAsync(
        Guid senderUserId,
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        if (pageNumber < 1 || pageSize < 1)
            throw new InvalidOperationException("pageNumber and pageSize must be greater than 0.");

        var sender = await _db.Users.FirstOrDefaultAsync(u => u.Id == senderUserId, ct);
        if (sender == null)
            throw new InvalidOperationException("Sender user not found.");

        if (sender.Role != AppRole.Admin && sender.Role != AppRole.SuperAdmin && sender.Role != AppRole.HR)
            throw new InvalidOperationException("Only Admin, HR or SuperAdmin can send direct notifications.");

        var query = _db.Users.Where(u => u.IsActive && u.Id != senderUserId);

        if (sender.Role == AppRole.Admin)
        {
            if (!sender.DepartmentId.HasValue)
                throw new InvalidOperationException("Admin does not belong to any department.");

            query = query.Where(u => u.DepartmentId == sender.DepartmentId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                (u.FirstNameAr != null && u.FirstNameAr.ToLower().Contains(term)) ||
                (u.MiddleNameAr != null && u.MiddleNameAr.ToLower().Contains(term)) ||
                (u.LastNameAr != null && u.LastNameAr.ToLower().Contains(term)) ||
                (u.FirstNameEn != null && u.FirstNameEn.ToLower().Contains(term)) ||
                (u.MiddleNameEn != null && u.MiddleNameEn.ToLower().Contains(term)) ||
                (u.LastNameEn != null && u.LastNameEn.ToLower().Contains(term)) ||
                (u.EmployeeCode != null && u.EmployeeCode.ToLower().Contains(term)) ||
                (u.PhoneNumber != null && u.PhoneNumber.ToLower().Contains(term)));
        }

        var users = await query
            .OrderBy(u => u.FirstNameEn ?? u.FirstNameAr ?? u.EmployeeCode ?? u.PhoneNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.EmployeeCode,
                u.FirstNameAr,
                u.MiddleNameAr,
                u.LastNameAr,
                u.FirstNameEn,
                u.MiddleNameEn,
                u.LastNameEn,
                u.DepartmentId
            })
            .ToListAsync(ct);

        var departmentIds = users
            .Where(x => x.DepartmentId.HasValue)
            .Select(x => x.DepartmentId!.Value)
            .Distinct()
            .ToList();

        var departments = await _db.Departments
            .Where(d => departmentIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Name, ct);

        return users.Select(u =>
        {
            var fullNameAr = string.Join(" ", new[] { u.FirstNameAr, u.MiddleNameAr, u.LastNameAr }
                .Where(n => !string.IsNullOrWhiteSpace(n)));
            var fullNameEn = string.Join(" ", new[] { u.FirstNameEn, u.MiddleNameEn, u.LastNameEn }
                .Where(n => !string.IsNullOrWhiteSpace(n)));

            return new NotificationUserLookupResponse
            {
                UserId = u.Id,
                EmployeeCode = u.EmployeeCode,
                FullNameAr = string.IsNullOrWhiteSpace(fullNameAr) ? null : fullNameAr,
                FullNameEn = string.IsNullOrWhiteSpace(fullNameEn) ? null : fullNameEn,
                DepartmentId = u.DepartmentId,
                DepartmentName = u.DepartmentId.HasValue && departments.TryGetValue(u.DepartmentId.Value, out var name) ? name : null
            };
        }).ToList();
    }

    public async Task SendDirectNotificationAsync(Guid senderUserId, Guid recipientUserId, string title, string message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidOperationException("Title is required.");
        if (string.IsNullOrWhiteSpace(message))
            throw new InvalidOperationException("Message is required.");
        if (senderUserId == recipientUserId)
            throw new InvalidOperationException("Cannot send direct notification to yourself.");

        var sender = await _db.Users.FirstOrDefaultAsync(u => u.Id == senderUserId, ct);
        if (sender == null)
            throw new InvalidOperationException("Sender user not found.");

        if (sender.Role != AppRole.Admin && sender.Role != AppRole.SuperAdmin && sender.Role != AppRole.HR)
            throw new InvalidOperationException("Only Admin, HR or SuperAdmin can send direct notifications.");

        var recipient = await _db.Users.FirstOrDefaultAsync(u => u.Id == recipientUserId && u.IsActive, ct);
        if (recipient == null)
            throw new InvalidOperationException("Recipient user not found or inactive.");

        if (sender.Role == AppRole.Admin)
        {
            if (!sender.DepartmentId.HasValue)
                throw new InvalidOperationException("Admin does not belong to any department.");

            if (recipient.DepartmentId != sender.DepartmentId)
                throw new InvalidOperationException("Admin can send direct notifications only within their department.");
        }

        var notification = new NotificationEntity
        {
            UserId = recipientUserId,
            Type = NotificationType.Direct,
            RequestId = 0,
            Message = $"{title}: {message}",
            IsRead = false,
            CreatedAt = DateTime.Now
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);

        try
        {
            var fcmTokens = await _db.FcmTokens
                .Where(t => t.UserId == recipientUserId)
                .Select(t => t.Token)
                .ToListAsync(ct);

            if (fcmTokens.Count > 0)
            {
                var data = new Dictionary<string, string>
                {
                    { "type", NotificationType.Direct.ToString() },
                    { "id", notification.Id.ToString() },
                    { "notificationId", notification.Id.ToString() },
                    { "senderUserId", senderUserId.ToString() }
                };

                await _fcmService.SendPushNotificationToMultipleAsync(
                    fcmTokens,
                    title.Trim(),
                    message.Trim(),
                    data,
                    ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send FCM direct notification to user {recipientUserId}");
        }
    }

    public async Task SendCustomNotificationAsync(
        NotificationType type,
        int requestId,
        string title,
        string message,
        IReadOnlyCollection<Guid> recipientUserIds,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default)
    {
        if (recipientUserIds == null || recipientUserIds.Count == 0)
            return;

        var recipients = await _db.Users
            .Where(u => recipientUserIds.Contains(u.Id) && u.IsActive)
            .Select(u => new { u.Id })
            .Distinct()
            .ToListAsync(ct);

        if (recipients.Count == 0)
            return;

        var notificationMessage = string.IsNullOrWhiteSpace(title) ? message.Trim() : $"{title.Trim()}: {message.Trim()}";

        var notifications = new List<NotificationEntity>();
        foreach (var recipient in recipients)
        {
            var notification = new NotificationEntity
            {
                UserId = recipient.Id,
                Type = type,
                RequestId = requestId,
                Message = notificationMessage,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _db.Notifications.Add(notification);
            notifications.Add(notification);
        }

        await _db.SaveChangesAsync(ct);

        for (var i = 0; i < recipients.Count; i++)
        {
            var recipient = recipients[i];
            var notification = notifications[i];

            try
            {
                var fcmTokens = await _db.FcmTokens
                    .Where(t => t.UserId == recipient.Id)
                    .Select(t => t.Token)
                    .ToListAsync(ct);

                if (fcmTokens.Count == 0)
                    continue;

                var payload = new Dictionary<string, string>
                {
                    ["type"] = type.ToString(),
                    ["requestId"] = requestId.ToString(),
                    ["id"] = requestId.ToString(),
                    ["notificationId"] = notification.Id.ToString()
                };

                if (data != null)
                {
                    foreach (var item in data)
                    {
                        payload[item.Key] = item.Value;
                    }
                }

                await _fcmService.SendPushNotificationToMultipleAsync(
                    fcmTokens,
                    string.IsNullOrWhiteSpace(title) ? "إشعار جديد" : title.Trim(),
                    message.Trim(),
                    payload,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send custom notification to user {UserId}", recipient.Id);
            }
        }
    }

    public async Task SendBroadcastNotificationAsync(Guid senderUserId, BroadcastNotificationRequest request, CancellationToken ct = default)
    {
        var sender = await _db.Users.FirstOrDefaultAsync(u => u.Id == senderUserId, ct);
        if (sender == null)
            throw new InvalidOperationException("المستخدم غير موجود");

        if (sender.Role != AppRole.SuperAdmin && sender.Role != AppRole.Admin && sender.Role != AppRole.HR)
            throw new InvalidOperationException("ليس لديك صلاحية لإرسال إشعارات عامة");

        var query = _db.Users.Where(u => u.IsActive);

        switch (request.TargetType)
        {
            case NotificationTargetType.All:
                // No additional filtering
                break;
            case NotificationTargetType.Admins:
                query = query.Where(u => u.Role == AppRole.Admin || u.Role == AppRole.SuperAdmin);
                break;
            case NotificationTargetType.Employees:
                query = query.Where(u => u.Role == AppRole.User);
                break;
            case NotificationTargetType.SpecificUser:
                if (!request.TargetUserId.HasValue)
                    throw new InvalidOperationException("يجب اختيار المستخدم");
                query = query.Where(u => u.Id == request.TargetUserId.Value);
                break;
            case NotificationTargetType.SpecificDepartment:
                if (!request.TargetDepartmentId.HasValue)
                    throw new InvalidOperationException("يجب اختيار القسم");
                query = query.Where(u => u.DepartmentId == request.TargetDepartmentId.Value);
                break;
            default:
                throw new InvalidOperationException("نوع المستهدف غير صالح");
        }

        var recipientIds = await query.Select(u => u.Id).ToListAsync(ct);

        if (recipientIds.Count == 0)
            return;

        await SendCustomNotificationAsync(
            NotificationType.Broadcast,
            0,
            request.Title,
            request.Message,
            recipientIds,
            new Dictionary<string, string> { { "senderUserId", senderUserId.ToString() } },
            ct);
    }
}

