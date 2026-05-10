using internalEmployee.Auth.Contracts;
using internalEmployee.Data.Entities;
using RequestStatus = internalEmployee.Data.Entities.RequestStatus;
using internalEmployee.Services.Overtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using OvertimeEntity = internalEmployee.Data.Entities.Overtime;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class OvertimeController : ControllerBase
{
    private readonly IOvertimeService _overtimeService;

    public OvertimeController(IOvertimeService overtimeService)
    {
        _overtimeService = overtimeService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(OvertimeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OvertimeResponse>> CreateOvertime([FromBody] OvertimeRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var overtime = await _overtimeService.CreateOvertimeAsync(userId, request, ct);
            var response = new OvertimeResponse
            {
                Id = overtime.Id,
                UserId = overtime.UserId,
                Date = DateOnly.FromDateTime(overtime.Date),
                StartTime = TimeOnly.FromDateTime(overtime.StartTime),
                EndTime = TimeOnly.FromDateTime(overtime.EndTime),
                TotalHours = overtime.TotalHours,
                HourlyRate = overtime.HourlyRate,
                Amount = overtime.Amount,
                Reason = overtime.Reason,
                CreatedAt = overtime.CreatedAt,
                Status = overtime.Status.ToString(),
                RejectionReason = overtime.RejectionReason
            };
            return CreatedAtAction(nameof(GetMyOvertimes), new { id = overtime.Id }, response);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(List<OvertimeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<OvertimeResponse>>> GetMyOvertimes(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var overtimes = await _overtimeService.GetUserOvertimesAsync(userId, ct);
        var responses = overtimes.Select(o => new OvertimeResponse
        {
            Id = o.Id,
            UserId = o.UserId,
            Date = DateOnly.FromDateTime(o.Date),
            StartTime = TimeOnly.FromDateTime(o.StartTime),
            EndTime = TimeOnly.FromDateTime(o.EndTime),
            TotalHours = o.TotalHours,
            HourlyRate = o.HourlyRate,
            Amount = o.Amount,
            Reason = o.Reason,
            CreatedAt = o.CreatedAt,
            Status = o.Status.ToString(),
            RejectionReason = o.RejectionReason
        }).ToList();
        return Ok(responses);
    }

    [HttpGet("statuses")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(List<StatusLookupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<List<StatusLookupResponse>> GetStatuses()
    {
        var statuses = Enum.GetValues(typeof(RequestStatus))
            .Cast<RequestStatus>()
            .Select(s => new StatusLookupResponse
            {
                Id = (int)s,
                Value = s.ToString()
            })
            .ToList();

        return Ok(statuses);
    }

    [HttpGet("pending")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(List<OvertimeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<OvertimeResponse>>> GetPendingOvertimes(CancellationToken ct)
    {
        var overtimes = await _overtimeService.GetPendingOvertimesAsync(ct);
        var responses = overtimes.Select(o => new OvertimeResponse
        {
            Id = o.Id,
            UserId = o.UserId,
            Date = DateOnly.FromDateTime(o.Date),
            StartTime = TimeOnly.FromDateTime(o.StartTime),
            EndTime = TimeOnly.FromDateTime(o.EndTime),
            TotalHours = o.TotalHours,
            HourlyRate = o.HourlyRate,
            Amount = o.Amount,
            Reason = o.Reason,
            CreatedAt = o.CreatedAt,
            Status = o.Status.ToString(),
            RejectionReason = o.RejectionReason
        }).ToList();
        return Ok(responses);
    }

    [HttpGet("all")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(List<OvertimeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<OvertimeResponse>>> GetAllOvertimes(
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        RequestStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<RequestStatus>(status, ignoreCase: true, out var s))
        {
            parsedStatus = s;
        }

        var overtimes = await _overtimeService.GetAllOvertimesAsync(parsedStatus, ct);
        var responses = overtimes.Select(o => new OvertimeResponse
        {
            Id = o.Id,
            UserId = o.UserId,
            Date = DateOnly.FromDateTime(o.Date),
            StartTime = TimeOnly.FromDateTime(o.StartTime),
            EndTime = TimeOnly.FromDateTime(o.EndTime),
            TotalHours = o.TotalHours,
            HourlyRate = o.HourlyRate,
            Amount = o.Amount,
            Reason = o.Reason,
            CreatedAt = o.CreatedAt,
            Status = o.Status.ToString(),
            RejectionReason = o.RejectionReason
        }).ToList();

        return Ok(responses);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "SuperAdmin,Admin")]
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
            var overtime = await _overtimeService.UpdateStatusAsync(id, currentUserId, request, ct);
            return Ok(new StatusUpdateResponse
            {
                Id = overtime.Id,
                Status = overtime.Status.ToString(),
                RejectionReason = overtime.RejectionReason
            });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }
}

public sealed class StatusLookupResponse
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
}
