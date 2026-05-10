using internalEmployee.Auth.Contracts;
using internalEmployee.Services.Custody;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,Admin,HR")]
public sealed class CustodyItemController : ControllerBase
{
    private readonly ICustodyService _custodyService;

    public CustodyItemController(ICustodyService custodyService)
    {
        _custodyService = custodyService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<CustodyItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CustodyItemResponse>>> GetAll([FromQuery] bool activeOnly = false, CancellationToken ct = default)
    {
        var items = await _custodyService.GetCustodyItemsAsync(activeOnly, ct);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CustodyItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustodyItemResponse>> GetById(int id, CancellationToken ct)
    {
        var item = await _custodyService.GetCustodyItemByIdAsync(id, ct);
        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CustodyItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustodyItemResponse>> Create([FromBody] CreateCustodyItemRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _custodyService.CreateCustodyItemAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CustodyItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustodyItemResponse>> Update(int id, [FromBody] UpdateCustodyItemRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _custodyService.UpdateCustodyItemAsync(id, request, ct);
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            var deleted = await _custodyService.DeleteCustodyItemAsync(id, ct);
            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }
}
