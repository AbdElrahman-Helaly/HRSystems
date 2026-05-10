using internalEmployee.Auth.Contracts;
using internalEmployee.Data.Entities;
using internalEmployee.Services.Penalty;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class EmployeePenaltyController : ControllerBase
{
    private readonly IPenaltyService _penaltyService;

    public EmployeePenaltyController(IPenaltyService penaltyService)
    {
        _penaltyService = penaltyService;
    }

    [HttpGet("types")]
    [ProducesResponseType(typeof(List<PenaltyTypeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PenaltyTypeResponse>>> GetPenaltyTypes(CancellationToken ct)
    {
        var types = await _penaltyService.GetPenaltyTypesAsync(ct);
        var response = types.Select(t => new PenaltyTypeResponse
        {
            Value = (int)t,
            Name = t.ToString(),
            DisplayName = t == PenaltyType.Days ? "أيام" : "مبلغ ثابت"
        }).ToList();

        return Ok(response);
    }

    [HttpGet("{employeeId}")]
    [ProducesResponseType(typeof(List<EmployeePenalty>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<EmployeePenalty>>> GetEmployeePenalties(Guid employeeId, CancellationToken ct)
    {
        try
        {
            var penalties = await _penaltyService.GetEmployeePenaltiesAsync(employeeId, ct);
            return Ok(penalties);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: ex.Message);
        }
    }

    [HttpGet("{employeeId}/pending")]
    [ProducesResponseType(typeof(List<EmployeePenalty>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EmployeePenalty>>> GetPendingPenalties(Guid employeeId, CancellationToken ct)
    {
        try
        {
            var penalties = await _penaltyService.GetPendingPenaltiesAsync(employeeId, ct);
            return Ok(penalties);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: ex.Message);
        }
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(EmployeePenalty), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EmployeePenalty>> CreatePenalty([FromBody] CreatePenaltyRequest request, CancellationToken ct)
    {
        try
        {
            // Get current user ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            var penalty = await _penaltyService.CreatePenaltyAsync(request, currentUserId, ct);
            return CreatedAtAction(nameof(GetEmployeePenalties), new { employeeId = penalty.UserId }, penalty);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(EmployeePenalty), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeePenalty>> UpdatePenalty(int id, [FromBody] UpdatePenaltyRequest request, CancellationToken ct)
    {
        try
        {
            var penalty = await _penaltyService.UpdatePenaltyAsync(id, request, ct);
            return Ok(penalty);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeletePenalty(int id, CancellationToken ct)
    {
        try
        {
            await _penaltyService.DeletePenaltyAsync(id, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }
}

public sealed class PenaltyTypeResponse
{
    public int Value { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
