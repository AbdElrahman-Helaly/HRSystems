using internalEmployee.Auth.Contracts;
using internalEmployee.Services.SalaryAdvance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using RequestStatus = internalEmployee.Data.Entities.RequestStatus;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class SalaryAdvanceController : ControllerBase
{
    private readonly ISalaryAdvanceService _salaryAdvanceService;

    public SalaryAdvanceController(ISalaryAdvanceService salaryAdvanceService)
    {
        _salaryAdvanceService = salaryAdvanceService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SalaryAdvanceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SalaryAdvanceResponse>> CreateAdvance([FromBody] SalaryAdvanceRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var advance = await _salaryAdvanceService.CreateAdvanceRequestAsync(userId, request, ct);
            return CreatedAtAction(nameof(GetMyAdvances), new { id = advance.Id }, MapResponse(advance));
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPost("manual")]
    [Authorize(Roles = "SuperAdmin,HR")]
    [ProducesResponseType(typeof(SalaryAdvanceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SalaryAdvanceResponse>> CreateManualAdvance([FromBody] SalaryAdvanceManualRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var advance = await _salaryAdvanceService.CreateManualAdvanceAsync(userId, request, ct);
            return CreatedAtAction(nameof(GetAllAdvances), new { id = advance.Id }, MapResponse(advance));
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(List<SalaryAdvanceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<SalaryAdvanceResponse>>> GetMyAdvances(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var advances = await _salaryAdvanceService.GetUserAdvancesAsync(userId, ct);
        return Ok(advances.Select(MapResponse).ToList());
    }

    [HttpGet("pending")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(List<SalaryAdvanceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<SalaryAdvanceResponse>>> GetPendingAdvances(CancellationToken ct)
    {
        var advances = await _salaryAdvanceService.GetPendingAdvancesAsync(ct);
        return Ok(advances.Select(MapResponse).ToList());
    }

    [HttpGet("all")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(List<SalaryAdvanceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<SalaryAdvanceResponse>>> GetAllAdvances(
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        RequestStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<RequestStatus>(status, ignoreCase: true, out var s))
        {
            parsedStatus = s;
        }

        var advances = await _salaryAdvanceService.GetAllAdvancesAsync(parsedStatus, ct);
        return Ok(advances.Select(MapResponse).ToList());
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
            var advance = await _salaryAdvanceService.UpdateStatusAsync(id, currentUserId, request, ct);
            return Ok(new StatusUpdateResponse
            {
                Id = advance.Id,
                Status = advance.Status.ToString(),
                RejectionReason = advance.RejectionReason
            });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    private static SalaryAdvanceResponse MapResponse(internalEmployee.Data.Entities.SalaryAdvance advance)
    {
        return new SalaryAdvanceResponse
        {
            Id = advance.Id,
            UserId = advance.UserId,
            Amount = advance.Amount,
            MonthlyDeduction = advance.MonthlyDeduction,
            NumberOfMonths = advance.NumberOfMonths,
            StartDate = DateOnly.FromDateTime(advance.StartDate),
            Reason = advance.Reason,
            Status = advance.Status.ToString(),
            RejectionReason = advance.RejectionReason,
            CreatedAt = advance.CreatedAt,
            ApprovedAt = advance.ApprovedAt
        };
    }
}
