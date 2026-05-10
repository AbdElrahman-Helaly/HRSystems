using internalEmployee.Auth.Contracts;
using internalEmployee.Data.Entities;
using internalEmployee.Services.Assignment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AssignmentEntity = internalEmployee.Data.Entities.Assignment;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AssignmentController : ControllerBase
{
    private readonly IAssignmentService _assignmentService;

    public AssignmentController(IAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AssignmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssignmentResponse>> CreateAssignment([FromBody] AssignmentRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var assignment = await _assignmentService.CreateAssignmentAsync(userId, request, ct);
            var response = new AssignmentResponse
            {
                Id = assignment.Id,
                UserId = assignment.UserId,
                Where = assignment.Where,
                StartDate = DateOnly.FromDateTime(assignment.StartTime),
                StartTime = TimeOnly.FromDateTime(assignment.StartTime),
                EndTime = TimeOnly.FromDateTime(assignment.EndTime),
                Reason = assignment.Reason,
                CreatedAt = assignment.CreatedAt,
                Status = assignment.Status.ToString(),
                RejectionReason = assignment.RejectionReason
            };
            return CreatedAtAction(nameof(GetMyAssignments), new { id = assignment.Id }, response);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPost("user/{userId}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(AssignmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssignmentResponse>> CreateAssignmentForUser(Guid userId, [FromBody] AssignmentRequest request, CancellationToken ct)
    {
        try
        {
            var assignment = await _assignmentService.CreateAssignmentAsync(userId, request, ct);
            var response = new AssignmentResponse
            {
                Id = assignment.Id,
                UserId = assignment.UserId,
                Where = assignment.Where,
                StartDate = DateOnly.FromDateTime(assignment.StartTime),
                StartTime = TimeOnly.FromDateTime(assignment.StartTime),
                EndTime = TimeOnly.FromDateTime(assignment.EndTime),
                Reason = assignment.Reason,
                CreatedAt = assignment.CreatedAt,
                Status = assignment.Status.ToString(),
                RejectionReason = assignment.RejectionReason
            };
            return Created("", response);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(List<AssignmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<AssignmentResponse>>> GetMyAssignments(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var assignments = await _assignmentService.GetUserAssignmentsAsync(userId, ct);
        var responses = assignments.Select(a => new AssignmentResponse
        {
            Id = a.Id,
            UserId = a.UserId,
            Where = a.Where,
            StartDate = DateOnly.FromDateTime(a.StartTime),
            StartTime = TimeOnly.FromDateTime(a.StartTime),
            EndTime = TimeOnly.FromDateTime(a.EndTime),
            Reason = a.Reason,
            CreatedAt = a.CreatedAt,
            Status = a.Status.ToString(),
            RejectionReason = a.RejectionReason
        }).ToList();
        return Ok(responses);
    }

    [HttpGet("all")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(PaginatedResponse<AssignmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResponse<AssignmentResponse>>> GetAllAssignments(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        CancellationToken ct = default)
    {
        // Parse optional status filter
        RequestStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<RequestStatus>(status, ignoreCase: true, out var s))
        {
            parsedStatus = s;
        }

        var result = await _assignmentService.GetAllAssignmentsPaginatedAsync(
            pageNumber, pageSize, search, parsedStatus, userId, dateFrom, dateTo, ct);

        return Ok(result);
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
            var assignment = await _assignmentService.UpdateStatusAsync(id, currentUserId, request, ct);
            return Ok(new StatusUpdateResponse
            {
                Id = assignment.Id,
                Status = assignment.Status.ToString(),
                RejectionReason = assignment.RejectionReason
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
            await _assignmentService.SendReminderAsync(id, currentUserId, ct);
            return Ok(new { message = "Reminder sent successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }
}

