using internalEmployee.Auth.Contracts;
using internalEmployee.Data.Entities;
using internalEmployee.Services.Recruitment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RecruitmentRequestController : ControllerBase
{
    private readonly IRecruitmentService _recruitmentService;

    public RecruitmentRequestController(IRecruitmentService recruitmentService)
    {
        _recruitmentService = recruitmentService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(RecruitmentRequestResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecruitmentRequestResponse>> Create(
        [FromBody] CreateRecruitmentRequest request,
        CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized();

        try
        {
            var result = await _recruitmentService.CreateRecruitmentRequestAsync(currentUserId, request, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpGet("my")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(PaginatedResponse<RecruitmentRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResponse<RecruitmentRequestResponse>>> GetMyRequests(
        [FromQuery] string? search,
        [FromQuery] RequestStatus? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized();

        try
        {
            var result = await _recruitmentService.GetMyRecruitmentRequestsAsync(
                currentUserId,
                search,
                status,
                pageNumber,
                pageSize,
                ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpGet("hr")]
    [Authorize(Roles = "HR,SuperAdmin")]
    [ProducesResponseType(typeof(PaginatedResponse<RecruitmentRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResponse<RecruitmentRequestResponse>>> GetHrInbox(
        [FromQuery] string? search,
        [FromQuery] RequestStatus? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized();

        try
        {
            var result = await _recruitmentService.GetHrRecruitmentRequestsAsync(
                currentUserId,
                search,
                status,
                pageNumber,
                pageSize,
                ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RecruitmentRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecruitmentRequestResponse>> GetById(int id, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized();

        try
        {
            var result = await _recruitmentService.GetRecruitmentRequestByIdAsync(id, currentUserId, ct);
            if (result == null)
                return NotFound();

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "HR,SuperAdmin")]
    [ProducesResponseType(typeof(RecruitmentRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecruitmentRequestResponse>> UpdateStatus(
        int id,
        [FromBody] UpdateRecruitmentRequestStatusRequest request,
        CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized();

        try
        {
            var result = await _recruitmentService.UpdateRecruitmentRequestStatusAsync(id, currentUserId, request, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpGet("{id:int}/candidates")]
    [ProducesResponseType(typeof(PaginatedResponse<RecruitmentCandidateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaginatedResponse<RecruitmentCandidateResponse>>> GetCandidates(
        int id,
        [FromQuery] string? search,
        [FromQuery] RequestStatus? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized();

        try
        {
            var result = await _recruitmentService.GetRecruitmentCandidatesAsync(
                id,
                currentUserId,
                search,
                status,
                pageNumber,
                pageSize,
                ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPost("{id:int}/candidates")]
    [Authorize(Roles = "HR,SuperAdmin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(RecruitmentCandidateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecruitmentCandidateResponse>> CreateCandidate(
        int id,
        [FromForm] CreateRecruitmentCandidateRequest request,
        CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized();

        try
        {
            var result = await _recruitmentService.CreateRecruitmentCandidateAsync(id, currentUserId, request, ct);
            return CreatedAtAction(nameof(RecruitmentCandidateController.GetById), "RecruitmentCandidate", new { id = result.Id }, result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    private bool TryGetCurrentUserId(out Guid currentUserId)
    {
        currentUserId = Guid.Empty;
        var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return !string.IsNullOrWhiteSpace(currentUserIdClaim) && Guid.TryParse(currentUserIdClaim, out currentUserId);
    }
}
