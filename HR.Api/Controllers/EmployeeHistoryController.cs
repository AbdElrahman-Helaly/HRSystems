using internalEmployee.Services.EmployeeHistory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class EmployeeHistoryController : ControllerBase
{
    private readonly IEmployeeHistoryService _historyService;

    public EmployeeHistoryController(IEmployeeHistoryService historyService)
    {
        _historyService = historyService;
    }

    /// <summary>
    /// Create a new employee history entry
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(EmployeeHistoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EmployeeHistoryResponse>> CreateHistory(
        [FromBody] CreateEmployeeHistoryRequest request,
        CancellationToken ct)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid? doneBy = null;
            if (!string.IsNullOrWhiteSpace(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
            {
                doneBy = userId;
            }

            var history = await _historyService.CreateHistoryAsync(request, doneBy, ct);
            var response = await _historyService.GetHistoryByIdAsync(history.Id, ct);

            return CreatedAtAction(nameof(GetHistoryById), new { id = history.Id }, response);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: ex.Message);
        }
    }

    /// <summary>
    /// Get employee history with filters and pagination
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(EmployeeHistoryListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EmployeeHistoryListResponse>> GetHistory(
        [FromQuery] Guid? employeeId,
        [FromQuery] Data.Entities.EmployeeEventType? eventType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        try
        {
            var request = new GetEmployeeHistoryRequest
            {
                EmployeeId = employeeId,
                EventType = eventType,
                StartDate = startDate,
                EndDate = endDate,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _historyService.GetEmployeeHistoryAsync(request, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: ex.Message);
        }
    }

    /// <summary>
    /// Get history for a specific employee
    /// </summary>
    [HttpGet("{employeeId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(EmployeeSpecificHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EmployeeSpecificHistoryResponse>> GetHistoryByEmployeeId(
        Guid employeeId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        try
        {
            var request = new GetEmployeeHistoryRequest
            {
                EmployeeId = employeeId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _historyService.GetEmployeeHistoryAsync(request, ct);

            var response = new EmployeeSpecificHistoryResponse
            {
                EmployeeName = result.Items.FirstOrDefault()?.EmployeeName,
                TotalCount = result.TotalCount,
                History = result.Items.Select(h => new EmployeeHistoryItemDto
                {
                    Id = h.Id,
                    EventType = h.EventType,
                    Description = h.Description,
                    Changes = h.Changes,
                    Date = h.Date,
                    DoneBy = h.DoneBy,
                    Reason = h.Reason,
                    Notes = h.Notes
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: ex.Message);
        }
    }

    /// <summary>
    /// Get specific history entry by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(EmployeeHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EmployeeHistoryResponse>> GetHistoryById(
        int id,
        CancellationToken ct)
    {
        try
        {
            var history = await _historyService.GetHistoryByIdAsync(id, ct);
            if (history == null)
                return NotFound();

            return Ok(history);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: ex.Message);
        }
    }

    /// <summary>
    /// Get employee history summary
    /// </summary>
    [HttpGet("summary/{employeeId}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(EmployeeHistorySummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EmployeeHistorySummaryResponse>> GetEmployeeSummary(
        Guid employeeId,
        CancellationToken ct)
    {
        try
        {
            var summary = await _historyService.GetEmployeeHistorySummaryAsync(employeeId, ct);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: ex.Message);
        }
    }

    /// <summary>
    /// Get my own history (for employees to view their own history)
    /// </summary>
    [HttpGet("my-history")]
    [ProducesResponseType(typeof(EmployeeSpecificHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EmployeeSpecificHistoryResponse>> GetMyHistory(
        [FromQuery] Data.Entities.EmployeeEventType? eventType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var request = new GetEmployeeHistoryRequest
            {
                EmployeeId = userId,
                EventType = eventType,
                StartDate = startDate,
                EndDate = endDate,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _historyService.GetEmployeeHistoryAsync(request, ct);

            var response = new EmployeeSpecificHistoryResponse
            {
                EmployeeName = result.Items.FirstOrDefault()?.EmployeeName,
                TotalCount = result.TotalCount,
                History = result.Items.Select(h => new EmployeeHistoryItemDto
                {
                    Id = h.Id,
                    EventType = h.EventType,
                    Description = h.Description,
                    Changes = h.Changes,
                    Date = h.Date,
                    DoneBy = h.DoneBy,
                    Reason = h.Reason,
                    Notes = h.Notes
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: ex.Message);
        }
    }

    /// <summary>
    /// Get my own history summary
    /// </summary>
    [HttpGet("my-summary")]
    [ProducesResponseType(typeof(EmployeeHistorySummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EmployeeHistorySummaryResponse>> GetMySummary(CancellationToken ct)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var summary = await _historyService.GetEmployeeHistorySummaryAsync(userId, ct);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: ex.Message);
        }
    }
}
