using internalEmployee.Auth.Contracts;
using internalEmployee.Services.PublicHoliday;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PublicHolidayController : ControllerBase
{
    private readonly IPublicHolidayService _publicHolidayService;

    public PublicHolidayController(IPublicHolidayService publicHolidayService)
    {
        _publicHolidayService = publicHolidayService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<PublicHolidayResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<PublicHolidayResponse>>> GetAll([FromQuery] int? year, CancellationToken ct)
    {
        try
        {
            var holidays = await _publicHolidayService.GetAllAsync(year, ct);
            return Ok(holidays);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: ex.Message);
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PublicHolidayResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PublicHolidayResponse>> GetById(int id, CancellationToken ct)
    {
        try
        {
            var holiday = await _publicHolidayService.GetByIdAsync(id, ct);
            if (holiday == null)
                return NotFound();

            return Ok(holiday);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: ex.Message);
        }
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(PublicHolidayResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PublicHolidayResponse>> Create([FromBody] CreatePublicHolidayRequest request, CancellationToken ct)
    {
        try
        {
            var holiday = await _publicHolidayService.CreateAsync(request, ct);
            var response = await _publicHolidayService.GetByIdAsync(holiday.Id, ct);
            return CreatedAtAction(nameof(GetById), new { id = holiday.Id }, response);
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

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(PublicHolidayResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PublicHolidayResponse>> Update(int id, [FromBody] UpdatePublicHolidayRequest request, CancellationToken ct)
    {
        try
        {
            var holiday = await _publicHolidayService.UpdateAsync(id, request, ct);
            var response = await _publicHolidayService.GetByIdAsync(holiday.Id, ct);
            return Ok(response);
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

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            await _publicHolidayService.DeleteAsync(id, ct);
            return NoContent();
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

    [HttpPost("{holidayId}/exceptions")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(PublicHolidayExceptionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PublicHolidayExceptionResponse>> AddException(int holidayId, [FromBody] CreatePublicHolidayExceptionRequest request, CancellationToken ct)
    {
        try
        {
            var exception = await _publicHolidayService.AddExceptionAsync(holidayId, request, ct);
            var exceptions = await _publicHolidayService.GetExceptionsAsync(holidayId, ct);
            var response = exceptions.FirstOrDefault(e => e.Id == exception.Id);
            if (response == null)
                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Failed to retrieve created exception");

            return CreatedAtAction(nameof(GetExceptions), new { holidayId }, response);
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

    [HttpGet("{holidayId}/exceptions")]
    [ProducesResponseType(typeof(List<PublicHolidayExceptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<PublicHolidayExceptionResponse>>> GetExceptions(int holidayId, CancellationToken ct)
    {
        try
        {
            var exceptions = await _publicHolidayService.GetExceptionsAsync(holidayId, ct);
            return Ok(exceptions);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: ex.Message);
        }
    }

    [HttpDelete("exceptions/{exceptionId}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteException(int exceptionId, CancellationToken ct)
    {
        try
        {
            await _publicHolidayService.DeleteExceptionAsync(exceptionId, ct);
            return NoContent();
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
}
