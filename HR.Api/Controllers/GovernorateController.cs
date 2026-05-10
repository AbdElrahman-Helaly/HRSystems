using internalEmployee.Data;
using internalEmployee.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class GovernorateController : ControllerBase
{
    private readonly AppDbContext _db;

    public GovernorateController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<Governorate>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Governorate>>> GetGovernorates(CancellationToken ct)
    {
        var governorates = await _db.Governorates
            .Where(g => g.IsActive)
            .OrderBy(g => g.Name)
            .ToListAsync(ct);
        
        return Ok(governorates);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(Governorate), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Governorate>> AddGovernorate([FromBody] CreateGovernorateRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Name is required.");

        var name = request.Name.Trim();

        var exists = await _db.Governorates
            .AnyAsync(g => g.Name == name, ct);
        if (exists)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Governorate with the same name already exists.");

        var governorate = new Governorate
        {
            Name = name,
            NameAr = request.NameAr?.Trim(),
            IsActive = request.IsActive ?? true
        };

        _db.Governorates.Add(governorate);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetGovernorates), new { id = governorate.Id }, governorate);
    }
}

public sealed class CreateGovernorateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public bool? IsActive { get; set; }
}

