using internalEmployee.Auth.Contracts;
using internalEmployee.Data.Entities;
using internalEmployee.Services.Permission;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PermissionEntity = internalEmployee.Data.Entities.Permission;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PermissionController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public PermissionController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(PermissionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PermissionResponse>> CreatePermission([FromBody] PermissionRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var permission = await _permissionService.CreatePermissionAsync(userId, request, ct);
            var response = new PermissionResponse
            {
                Id = permission.Id,
                UserId = permission.UserId,
                Date = DateOnly.FromDateTime(permission.Date),
                StartTime = TimeOnly.FromDateTime(permission.StartTime),
                EndTime = TimeOnly.FromDateTime(permission.EndTime),
                Reason = permission.Reason,
                CreatedAt = permission.CreatedAt,
                Status = permission.Status.ToString(),
                RejectionReason = permission.RejectionReason
            };
            return CreatedAtAction(nameof(GetMyPermissions), new { id = permission.Id }, response);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPost("user/{userId}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(PermissionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PermissionResponse>> CreatePermissionForUser(Guid userId, [FromBody] PermissionRequest request, CancellationToken ct)
    {
        try
        {
            var permission = await _permissionService.CreatePermissionAsync(userId, request, ct);
            var response = new PermissionResponse
            {
                Id = permission.Id,
                UserId = permission.UserId,
                Date = DateOnly.FromDateTime(permission.Date),
                StartTime = TimeOnly.FromDateTime(permission.StartTime),
                EndTime = TimeOnly.FromDateTime(permission.EndTime),
                Reason = permission.Reason,
                CreatedAt = permission.CreatedAt,
                Status = permission.Status.ToString(),
                RejectionReason = permission.RejectionReason
            };
            return Created("", response);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(List<PermissionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<PermissionResponse>>> GetMyPermissions(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var permissions = await _permissionService.GetUserPermissionsAsync(userId, ct);
        var responses = permissions.Select(p => new PermissionResponse
        {
            Id = p.Id,
            UserId = p.UserId,
            Date = DateOnly.FromDateTime(p.Date),
            StartTime = TimeOnly.FromDateTime(p.StartTime),
            EndTime = TimeOnly.FromDateTime(p.EndTime),
            Reason = p.Reason,
            CreatedAt = p.CreatedAt,
            Status = p.Status.ToString(),
            RejectionReason = p.RejectionReason
        }).ToList();
        return Ok(responses);
    }

    [HttpGet("all")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(List<PermissionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<PermissionResponse>>> GetAllPermissions(CancellationToken ct)
    {
        var permissions = await _permissionService.GetAllPermissionsAsync(ct);
        var responses = permissions.Select(p => new PermissionResponse
        {
            Id = p.Id,
            UserId = p.UserId,
            Date = DateOnly.FromDateTime(p.Date),
            StartTime = TimeOnly.FromDateTime(p.StartTime),
            EndTime = TimeOnly.FromDateTime(p.EndTime),
            Reason = p.Reason,
            CreatedAt = p.CreatedAt,
            Status = p.Status.ToString(),
            RejectionReason = p.RejectionReason
        }).ToList();
        return Ok(responses);
    }



    [HttpPut("{id}/status")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(StatusUpdateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StatusUpdateResponse>> UpdateStatus(int id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
            return Unauthorized();

        try
        {
            var permission = await _permissionService.UpdateStatusAsync(id, currentUserId, request, ct);
            return Ok(new StatusUpdateResponse
            {
                Id = permission.Id,
                Status = permission.Status.ToString(),
                RejectionReason = permission.RejectionReason
            });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPost("{id}/remind")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Remind(int id, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
            return Unauthorized();

        try
        {
            await _permissionService.SendReminderAsync(id, currentUserId, ct);
            return Ok(new { message = "Reminder sent successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }
}

