using internalEmployee.Auth;
using internalEmployee.Auth.Contracts;
using internalEmployee.Data.Entities;
using internalEmployee.Services.WorkFromHome;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace internalEmployee.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WorkFromHomeController : ControllerBase
{
    private readonly IWorkFromHomeService _wfhService;

    public WorkFromHomeController(IWorkFromHomeService wfhService)
    {
        _wfhService = wfhService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateRequest([FromBody] WorkFromHomeCreateRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            var result = await _wfhService.CreateRequestAsync(userId, request, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyRequests(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var requests = await _wfhService.GetUserRequestsAsync(userId, ct);
        return Ok(requests);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            var result = await _wfhService.UpdateStatusAsync(id, currentUserId, request, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("department")]
    public async Task<IActionResult> GetDepartmentRequests(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] RequestStatus? status = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        CancellationToken ct = default)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            var requests = await _wfhService.GetDepartmentRequestsPaginatedAsync(
                currentUserId, pageNumber, pageSize, search, status, userId, dateFrom, dateTo, ct);
            return Ok(requests);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
