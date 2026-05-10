using internalEmployee.Auth.Contracts;
using internalEmployee.Services.Recruitment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RecruitmentCandidateController : ControllerBase
{
    private readonly IRecruitmentService _recruitmentService;

    public RecruitmentCandidateController(IRecruitmentService recruitmentService)
    {
        _recruitmentService = recruitmentService;
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RecruitmentCandidateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecruitmentCandidateResponse>> GetById(int id, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized();

        try
        {
            var result = await _recruitmentService.GetRecruitmentCandidateByIdAsync(id, currentUserId, ct);
            if (result == null)
                return NotFound();

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpGet("{id:int}/cv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCv(int id, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized();

        try
        {
            var file = await _recruitmentService.GetRecruitmentCandidateCvAsync(id, currentUserId, ct);
            return PhysicalFile(file.FilePath, file.ContentType, file.FileName);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(RecruitmentCandidateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecruitmentCandidateResponse>> UpdateStatus(
        int id,
        [FromBody] UpdateRecruitmentCandidateStatusRequest request,
        CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized();

        try
        {
            var result = await _recruitmentService.UpdateRecruitmentCandidateStatusAsync(id, currentUserId, request, ct);
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

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "HR,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized();

        try
        {
            var deleted = await _recruitmentService.DeleteRecruitmentCandidateAsync(id, currentUserId, ct);
            if (!deleted)
                return NotFound();

            return NoContent();
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
