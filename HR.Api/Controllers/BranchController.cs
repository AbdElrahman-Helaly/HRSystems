using internalEmployee.Data;
using internalEmployee.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class BranchController : ControllerBase
{
    private readonly AppDbContext _db;

    public BranchController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<Branch>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Branch>>> GetBranches(CancellationToken ct)
    {
        var branches = await _db.Branches
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .ToListAsync(ct);
        
        return Ok(branches);
    }
}

