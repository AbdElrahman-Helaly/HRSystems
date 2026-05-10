using internalEmployee.Data;
using internalEmployee.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class NationalityController : ControllerBase
{
    private readonly AppDbContext _db;

    public NationalityController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<Nationality>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Nationality>>> GetNationalities(CancellationToken ct)
    {
        var nationalities = await _db.Nationalities
            .OrderBy(n => n.Name)
            .ToListAsync(ct);
        
        return Ok(nationalities);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(Nationality), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Nationality>> AddNationality([FromBody] CreateNationalityRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Name is required.");

        var name = request.Name.Trim();

        var exists = await _db.Nationalities
            .AnyAsync(n => n.Name == name, ct);
        if (exists)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Nationality with the same name already exists.");

        var nationality = new Nationality
        {
            Name = name,
            NameAr = request.NameAr?.Trim()
        };

        _db.Nationalities.Add(nationality);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetNationalities), new { id = nationality.Id }, nationality);
    }
}

public sealed class CreateNationalityRequest
{
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
}

