using internalEmployee.Auth.Contracts;
using internalEmployee.Data;
using internalEmployee.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DepartmentController : ControllerBase
{
    private readonly AppDbContext _db;

    public DepartmentController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<Department>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<Department>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var query = _db.Departments.OrderBy(d => d.Name);
        
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(new PaginatedResponse<Department>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(Department), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Department>> AddDepartment([FromBody] CreateDepartmentRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Name is required.");

        var name = request.Name.Trim();

        var exists = await _db.Departments
            .AnyAsync(d => d.Name == name, ct);
        if (exists)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Department with the same name already exists.");

        var department = new Department
        {
            Name = name
        };

        _db.Departments.Add(department);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetAll), new { id = department.Id }, department);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteDepartment(int id, CancellationToken ct)
    {
        var department = await _db.Departments.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (department == null)
            return NotFound();

        var hasUsers = await _db.Users.AnyAsync(u => u.DepartmentId == id, ct);
        if (hasUsers)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Department has employees assigned.");

        var hasMeetings = await _db.MeetingDepartments.AnyAsync(md => md.DepartmentId == id, ct);
        if (hasMeetings)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Department is linked to meetings.");

        var hasHolidayExceptions = await _db.PublicHolidayExceptions.AnyAsync(e => e.DepartmentId == id, ct);
        if (hasHolidayExceptions)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Department is used in public holiday exceptions.");

        _db.Departments.Remove(department);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}

public sealed class CreateDepartmentRequest
{
    public string Name { get; set; } = string.Empty;
}

