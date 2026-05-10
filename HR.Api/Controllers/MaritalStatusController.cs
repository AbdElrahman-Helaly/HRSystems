using internalEmployee.Data;
using internalEmployee.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class MaritalStatusController : ControllerBase
{
    private readonly AppDbContext _db;

    public MaritalStatusController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<MaritalStatus>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MaritalStatus>>> GetMaritalStatuses(CancellationToken ct)
    {
        var maritalStatuses = await _db.MaritalStatuses
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .ToListAsync(ct);
        
        return Ok(maritalStatuses);
    }
}

