using internalEmployee.Auth.Contracts;
using internalEmployee.Services.HealthInsurance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class HealthInsuranceController : ControllerBase
{
    private readonly IHealthInsuranceService _healthInsuranceService;

    public HealthInsuranceController(IHealthInsuranceService healthInsuranceService)
    {
        _healthInsuranceService = healthInsuranceService;
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,HR")]
    [ProducesResponseType(typeof(HealthInsuranceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HealthInsuranceResponse>> Create([FromBody] HealthInsuranceRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var enrollment = await _healthInsuranceService.CreateAsync(userId, request, ct);
            return CreatedAtAction(nameof(GetAll), new { id = enrollment.Id }, MapResponse(enrollment));
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpGet("employee/{id}")]
    [Authorize(Roles = "SuperAdmin,HR,Admin")]
    [ProducesResponseType(typeof(List<HealthInsuranceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<HealthInsuranceResponse>>> GetByEmployee(Guid id, CancellationToken ct)
    {
        var list = await _healthInsuranceService.GetByUserAsync(id, ct);
        return Ok(list.Select(MapResponse).ToList());
    }

    [HttpGet("all")]
    [Authorize(Roles = "SuperAdmin,HR")]
    [ProducesResponseType(typeof(List<HealthInsuranceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<HealthInsuranceResponse>>> GetAll(CancellationToken ct)
    {
        var list = await _healthInsuranceService.GetAllAsync(ct);
        return Ok(list.Select(MapResponse).ToList());
    }

    [HttpPut("{id}/deactivate")]
    [Authorize(Roles = "SuperAdmin,HR")]
    [ProducesResponseType(typeof(HealthInsuranceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HealthInsuranceResponse>> Deactivate(int id, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var enrollment = await _healthInsuranceService.DeactivateAsync(id, userId, ct);
            return Ok(MapResponse(enrollment));
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    private static HealthInsuranceResponse MapResponse(internalEmployee.Data.Entities.HealthInsuranceEnrollment e)
    {
        return new HealthInsuranceResponse
        {
            Id = e.Id,
            UserId = e.UserId,
            MonthlyPremium = e.MonthlyPremium,
            StartDate = DateOnly.FromDateTime(e.StartDate),
            EndDate = e.EndDate.HasValue ? DateOnly.FromDateTime(e.EndDate.Value) : null,
            IsActive = e.IsActive,
            Notes = e.Notes,
            CreatedAt = e.CreatedAt
        };
    }
}
