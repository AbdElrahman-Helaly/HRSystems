using internalEmployee.Data;
using internalEmployee.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CityController : ControllerBase
{
    private readonly AppDbContext _db;

    public CityController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<City>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<City>>> GetCities([FromQuery] int? governorateId, CancellationToken ct)
    {
        var query = _db.Cities
            .Where(c => c.IsActive)
            .AsQueryable();

        if (governorateId.HasValue)
        {
            query = query.Where(c => c.GovernorateId == governorateId.Value);
        }

        var cities = await query
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
        
        return Ok(cities);
    }

    [HttpGet("by-governorate/{governorateId}")]
    [ProducesResponseType(typeof(List<City>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<City>>> GetCitiesByGovernorate(int governorateId, CancellationToken ct)
    {
        // Validate that governorate exists
        var governorateExists = await _db.Governorates
            .AnyAsync(g => g.Id == governorateId, ct);
        
        if (!governorateExists)
            return NotFound(new { message = "Governorate not found." });

        var cities = await _db.Cities
            .Where(c => c.GovernorateId == governorateId && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
        
        return Ok(cities);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(City), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<City>> AddCity([FromBody] CreateCityRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Name is required.");

        // Validate governorate
        var governorateExists = await _db.Governorates
            .AnyAsync(g => g.Id == request.GovernorateId, ct);
        if (!governorateExists)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid GovernorateId.");

        var name = request.Name.Trim();

        var exists = await _db.Cities
            .AnyAsync(c => c.GovernorateId == request.GovernorateId && c.Name == name, ct);
        if (exists)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "City with the same name already exists in this governorate.");

        var city = new City
        {
            GovernorateId = request.GovernorateId,
            Name = name,
            NameAr = request.NameAr?.Trim(),
            IsActive = request.IsActive ?? true
        };

        _db.Cities.Add(city);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetCities), new { id = city.Id }, city);
    }
}

public sealed class CreateCityRequest
{
    public int GovernorateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public bool? IsActive { get; set; }
}

