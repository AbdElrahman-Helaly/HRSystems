using internalEmployee.Auth.Contracts;
using internalEmployee.Services.Custody;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,Admin,HR")]
public sealed class EmployeeCustodyController : ControllerBase
{
    private readonly ICustodyService _custodyService;

    public EmployeeCustodyController(ICustodyService custodyService)
    {
        _custodyService = custodyService;
    }


    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<EmployeeCustodyResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<EmployeeCustodyResponse>>> GetAll(
        [FromQuery] Guid? userId,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var items = await _custodyService.GetEmployeeCustodiesAsync(userId, search, pageNumber, pageSize, ct);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EmployeeCustodyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeCustodyResponse>> GetById(int id, CancellationToken ct)
    {
        var item = await _custodyService.GetEmployeeCustodyByIdAsync(id, ct);
        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EmployeeCustodyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EmployeeCustodyResponse>> Create([FromBody] CreateEmployeeCustodyRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _custodyService.CreateEmployeeCustodyAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(EmployeeCustodyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeCustodyResponse>> Update(int id, [FromBody] UpdateEmployeeCustodyRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _custodyService.UpdateEmployeeCustodyAsync(id, request, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _custodyService.DeleteEmployeeCustodyAsync(id, ct);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
