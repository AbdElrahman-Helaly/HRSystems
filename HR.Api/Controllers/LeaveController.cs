using internalEmployee.Auth.Contracts;
using internalEmployee.Data.Entities;
using internalEmployee.Services.Leave;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using LeaveEntity = internalEmployee.Data.Entities.Leave;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class LeaveController : ControllerBase
{
    private readonly ILeaveService _leaveService;

    public LeaveController(ILeaveService leaveService)
    {
        _leaveService = leaveService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(LeaveResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LeaveResponse>> CreateLeave([FromForm] LeaveRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var leave = await _leaveService.CreateLeaveAsync(userId, request, request.MedicalReport, userId, ct);
            var response = new LeaveResponse
            {
                Id = leave.Id,
                UserId = leave.UserId,
                StartDate = DateOnly.FromDateTime(leave.StartDate),
                EndDate = DateOnly.FromDateTime(leave.EndDate),
                Reason = leave.Reason,
                CreatedAt = leave.CreatedAt,
                Status = leave.Status.ToString(),
                RejectionReason = leave.RejectionReason,
                LeaveType = leave.LeaveType.ToString(),
                MedicalReportUrl = leave.MedicalReportUrl
            };
            return CreatedAtAction(nameof(GetMyLeaves), new { id = leave.Id }, response);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPost("user/{userId}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(LeaveResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LeaveResponse>> CreateLeaveForUser(Guid userId, [FromForm] LeaveRequest request, CancellationToken ct)
    {
        var requesterClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(requesterClaim) || !Guid.TryParse(requesterClaim, out var requesterId))
            return Unauthorized();

        try
        {
            var leave = await _leaveService.CreateLeaveAsync(userId, request, request.MedicalReport, requesterId, ct);
            var response = new LeaveResponse
            {
                Id = leave.Id,
                UserId = leave.UserId,
                StartDate = DateOnly.FromDateTime(leave.StartDate),
                EndDate = DateOnly.FromDateTime(leave.EndDate),
                Reason = leave.Reason,
                CreatedAt = leave.CreatedAt,
                Status = leave.Status.ToString(),
                RejectionReason = leave.RejectionReason,
                LeaveType = leave.LeaveType.ToString(),
                MedicalReportUrl = leave.MedicalReportUrl
            };
            return Created("", response);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    /// <summary>
    /// Get all employees with their leave balance
    /// </summary>
    [HttpGet("employees/balance")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(PaginatedEmployeeLeaveBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedEmployeeLeaveBalanceResponse>> GetAllEmployeesWithLeaveBalance(
        [FromQuery] string? search,
        [FromQuery] Guid? userId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _leaveService.GetAllEmployeesWithLeaveBalanceAsync(search, userId, pageNumber, pageSize, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: ex.Message);
        }
    }

    /// <summary>
    /// Get leaves for a specific user by userId
    /// </summary>
    [HttpGet("user/{userId}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(List<LeaveResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<LeaveResponse>>> GetUserLeaves(Guid userId, CancellationToken ct)
    {
        try
        {
            var leaves = await _leaveService.GetUserLeavesAsync(userId, ct);
            var responses = leaves.Select(l => new LeaveResponse
            {
                Id = l.Id,
                UserId = l.UserId,
                StartDate = DateOnly.FromDateTime(l.StartDate),
                EndDate = DateOnly.FromDateTime(l.EndDate),
                Reason = l.Reason,
                CreatedAt = l.CreatedAt,
                Status = l.Status.ToString(),
                RejectionReason = l.RejectionReason,
                LeaveType = l.LeaveType.ToString(),
                MedicalReportUrl = l.MedicalReportUrl
            }).ToList();
            return Ok(responses);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: ex.Message);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: ex.Message);
        }
    }

    /// <summary>
    /// Get leave balance for the current user (from token)
    /// </summary>
    [HttpGet("my/balance")]
    [ProducesResponseType(typeof(LeaveBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LeaveBalanceResponse>> GetMyLeaveBalance(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var balance = await _leaveService.GetLeaveBalanceAsync(userId, ct);
            return Ok(balance);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: ex.Message);
        }
    }

    /// <summary>
    /// Get leaves for the current user (from token)
    /// </summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(List<LeaveResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<LeaveResponse>>> GetMyLeaves(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var leaves = await _leaveService.GetUserLeavesAsync(userId, ct);
            var responses = leaves.Select(l => new LeaveResponse
            {
                Id = l.Id,
                UserId = l.UserId,
                StartDate = DateOnly.FromDateTime(l.StartDate),
                EndDate = DateOnly.FromDateTime(l.EndDate),
                Reason = l.Reason,
                CreatedAt = l.CreatedAt,
                Status = l.Status.ToString(),
                RejectionReason = l.RejectionReason,
                LeaveType = l.LeaveType.ToString(),
                MedicalReportUrl = l.MedicalReportUrl
            }).ToList();
            return Ok(responses);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    /// <summary>
    /// Get leaves and leave balance for the current user (from token)
    /// </summary>
    [HttpGet("my/with-balance")]
    [ProducesResponseType(typeof(MyLeavesWithBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MyLeavesWithBalanceResponse>> GetMyLeavesWithBalance(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var leaves = await _leaveService.GetUserLeavesAsync(userId, ct);
            var responses = leaves.Select(l => new LeaveResponse
            {
                Id = l.Id,
                UserId = l.UserId,
                StartDate = DateOnly.FromDateTime(l.StartDate),
                EndDate = DateOnly.FromDateTime(l.EndDate),
                Reason = l.Reason,
                CreatedAt = l.CreatedAt,
                Status = l.Status.ToString(),
                RejectionReason = l.RejectionReason,
                LeaveType = l.LeaveType.ToString(),
                MedicalReportUrl = l.MedicalReportUrl
            }).ToList();

            var balance = await _leaveService.GetLeaveBalanceAsync(userId, ct);

            return Ok(new MyLeavesWithBalanceResponse
            {
                Balance = balance,
                Leaves = responses
            });
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpGet("pending")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(List<LeaveResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<LeaveResponse>>> GetAllPendingLeaves(CancellationToken ct)
    {
        var leaves = await _leaveService.GetAllPendingLeavesAsync(ct);
        var responses = leaves.Select(l => new LeaveResponse
        {
            Id = l.Id,
            UserId = l.UserId,
            StartDate = DateOnly.FromDateTime(l.StartDate),
            EndDate = DateOnly.FromDateTime(l.EndDate),
            Reason = l.Reason,
            CreatedAt = l.CreatedAt,
            Status = l.Status.ToString(),
            RejectionReason = l.RejectionReason,
            LeaveType = l.LeaveType.ToString(),
            MedicalReportUrl = l.MedicalReportUrl
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
            var leave = await _leaveService.UpdateStatusAsync(id, currentUserId, request, ct);
            return Ok(new StatusUpdateResponse
            {
                Id = leave.Id,
                Status = leave.Status.ToString(),
                RejectionReason = leave.RejectionReason
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
            await _leaveService.SendReminderAsync(id, currentUserId, ct);
            return Ok(new { message = "Reminder sent successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    /// <summary>
    /// Update sick leave balance for an employee
    /// </summary>
    [HttpPut("sick-leave-balance")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateSickLeaveBalance([FromBody] UpdateSickLeaveBalanceRequest request, CancellationToken ct)
    {
        try
        {
            await _leaveService.UpdateSickLeaveBalanceAsync(
                request.EmployeeId, 
                request.SickLeaveBalance, 
                request.StartDate, 
                request.EndDate, 
                ct);
            return Ok(new { message = "Sick leave balance updated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: ex.Message);
        }
    }

    /// <summary>
    /// Get all leave types
    /// </summary>
    [HttpGet("types")]
    [ProducesResponseType(typeof(List<LeaveTypeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<List<LeaveTypeResponse>> GetAllLeaveTypes()
    {
        var leaveTypes = Enum.GetValues<LeaveType>()
            .Select(lt => new LeaveTypeResponse
            {
                Id = (int)lt,
                Name = lt.ToString(),
                NameAr = lt switch
                {
                    LeaveType.Annual => "إجازة سنوية",
                    LeaveType.Casual => "إجازة عرضية",
                    LeaveType.Sick => "إجازة مرضية",
                    LeaveType.Maternity => "إجازة وضع",
                    LeaveType.Paternity => "إجازة أبوة",
                    LeaveType.Hajj => "إجازة حج",
                    LeaveType.Exam => "إجازة امتحانات",
                    _ => lt.ToString()
                }
            })
            .ToList();

        return Ok(leaveTypes);
    }
}

