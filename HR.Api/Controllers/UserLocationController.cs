using internalEmployee.Auth.Contracts;
using internalEmployee.Services.UserLocation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,Admin,HR")]
public sealed class UserLocationController : ControllerBase
{
    private readonly IUserLocationService _service;

    public UserLocationController(IUserLocationService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserLocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserLocationResponse>> Create([FromBody] CreateUserLocationRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _service.CreateAsync(request, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UserLocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserLocationResponse>> Update(int id, [FromBody] UpdateUserLocationRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request, ct);
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
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    [HttpGet("by-user/{userId:guid}")]
    [ProducesResponseType(typeof(List<UserLocationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserLocationResponse>>> GetByUser(Guid userId, CancellationToken ct)
    {
        var items = await _service.GetByUserAsync(userId, ct);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserLocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserLocationResponse>> GetById(int id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(id, ct);
        if (item == null)
            return NotFound();

        return Ok(item);
    }
}

