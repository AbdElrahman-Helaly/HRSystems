using internalEmployee.Data;
using internalEmployee.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class EmploymentModeController : ControllerBase
{
    private readonly AppDbContext _db;

    public EmploymentModeController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<EmploymentMode>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EmploymentMode>>> GetEmploymentModes(CancellationToken ct)
    {
        var employmentModes = await _db.EmploymentModes
            .Where(e => e.IsActive)
            .OrderBy(e => e.Name)
            .ToListAsync(ct);
        
        return Ok(employmentModes);
    }
}

