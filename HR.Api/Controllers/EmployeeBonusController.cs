using internalEmployee.Auth.Contracts;
using internalEmployee.Data.Entities;
using internalEmployee.Services.Bonus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class EmployeeBonusController : ControllerBase
{
    private readonly IBonusService _bonusService;

    public EmployeeBonusController(IBonusService bonusService)
    {
        _bonusService = bonusService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<EmployeeBonus>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EmployeeBonus>>> GetAllBonuses(CancellationToken ct)
    {
        var bonuses = await _bonusService.GetAllBonusesAsync(ct);
        return Ok(bonuses);
    }

    [HttpGet("{employeeId}")]
    [ProducesResponseType(typeof(List<EmployeeBonus>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EmployeeBonus>>> GetEmployeeBonuses(Guid employeeId, CancellationToken ct)
    {
        var bonuses = await _bonusService.GetEmployeeBonusesAsync(employeeId, ct);
        return Ok(bonuses);
    }

    [HttpGet("details/{id}")]
    [ProducesResponseType(typeof(EmployeeBonus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeBonus>> GetBonusById(int id, CancellationToken ct)
    {
        try
        {
            var bonus = await _bonusService.GetBonusByIdAsync(id, ct);
            return Ok(bonus);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: ex.Message);
        }
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(EmployeeBonus), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EmployeeBonus>> CreateBonus([FromBody] CreateBonusRequest request, CancellationToken ct)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            var bonus = await _bonusService.CreateBonusAsync(request, currentUserId, ct);
            return CreatedAtAction(nameof(GetBonusById), new { id = bonus.Id }, bonus);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(EmployeeBonus), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeBonus>> UpdateBonus(int id, [FromBody] UpdateBonusRequest request, CancellationToken ct)
    {
        try
        {
            var bonus = await _bonusService.UpdateBonusAsync(id, request, ct);
            return Ok(bonus);
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
    public async Task<ActionResult> DeleteBonus(int id, CancellationToken ct)
    {
        try
        {
            await _bonusService.DeleteBonusAsync(id, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }
}
