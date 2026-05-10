using internalEmployee.Auth.Contracts;
using internalEmployee.Data.Entities;
using internalEmployee.Services.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<NotificationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<NotificationResponse>>> GetMyNotifications(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var notifications = await _notificationService.GetUserNotificationsAsync(userId, ct);
        var responses = notifications.Select(n => new NotificationResponse
        {
            Id = n.Id,
            UserId = n.UserId,
            Type = n.Type.ToString(),
            RequestId = n.RequestId,
            Message = n.Message,
            IsRead = n.IsRead,
            IsConfirmed = n.IsConfirmed,
            ConfirmedAt = n.ConfirmedAt,
            CreatedAt = n.CreatedAt
        }).ToList();

        return Ok(responses);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var count = await _notificationService.GetUnreadCountAsync(userId, ct);
        return Ok(new { count });
    }

    [HttpPut("{id}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> MarkAsRead(int id, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        await _notificationService.MarkNotificationAsReadAsync(id, userId, ct);
        return Ok(new { message = "Notification marked as read." });
    }

    [HttpPut("{id}/confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Confirm(int id, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        await _notificationService.ConfirmNotificationAsync(id, userId, ct);
        return Ok(new { message = "Notification confirmed successfully." });
    }

    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        await _notificationService.MarkAllNotificationsAsReadAsync(userId, ct);
        return Ok(new { message = "All notifications marked as read." });
    }

    [HttpPost("fcm-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RegisterFcmToken([FromBody] RegisterFcmTokenRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { message = "Token is required." });

        await _notificationService.RegisterFcmTokenAsync(userId, request.Token, request.DeviceInfo, ct);
        return Ok(new { message = "FCM token registered successfully." });
    }

    [HttpDelete("fcm-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UnregisterFcmToken([FromQuery] string token, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { message = "Token is required." });

        await _notificationService.UnregisterFcmTokenAsync(userId, token, ct);
        return Ok(new { message = "FCM token unregistered successfully." });
    }

    [HttpGet("users/search")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(List<NotificationUserLookupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<NotificationUserLookupResponse>>> SearchUsersForDirectNotification(
        [FromQuery] string? search = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var users = await _notificationService.SearchUsersForDirectNotificationAsync(userId, search, pageNumber, pageSize, ct);
            return Ok(users);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpGet("target-types")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public ActionResult GetTargetTypes()
    {
        var types = new[]
        {
            new { Id = (int)NotificationTargetType.All, Name = "الكل" },
            new { Id = (int)NotificationTargetType.Admins, Name = "المدراء" },
            new { Id = (int)NotificationTargetType.Employees, Name = "الموظفين" },
            new { Id = (int)NotificationTargetType.SpecificUser, Name = "شخص معين" },
            new { Id = (int)NotificationTargetType.SpecificDepartment, Name = "قسم معين" }
        };
        return Ok(types);
    }

    [HttpPost("direct")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> SendDirectNotification([FromBody] DirectNotificationRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var senderUserId))
            return Unauthorized();

        try
        {
            await _notificationService.SendDirectNotificationAsync(
                senderUserId,
                request.RecipientUserId,
                request.Title,
                request.Message,
                ct);

            return Ok(new { message = "Direct notification sent successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPost("broadcast")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> SendBroadcastNotification([FromBody] BroadcastNotificationRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var senderUserId))
            return Unauthorized();

        try
        {
            await _notificationService.SendBroadcastNotificationAsync(senderUserId, request, ct);
            return Ok(new { message = "Broadcast notification task started." });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }
}