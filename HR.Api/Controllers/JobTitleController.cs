using internalEmployee.Data;
using internalEmployee.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class JobTitleController : ControllerBase
{
    private readonly AppDbContext _db;

    public JobTitleController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<JobTitle>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<JobTitle>>> GetJobTitles(CancellationToken ct)
    {
        var jobTitles = await _db.JobTitles
            .OrderBy(j => j.Name)
            .ToListAsync(ct);
        
        return Ok(jobTitles);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(JobTitle), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JobTitle>> AddJobTitle([FromBody] CreateJobTitleRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Name is required.");

        var name = request.Name.Trim();

        // Optional: uniqueness check by name
        var exists = await _db.JobTitles
            .AnyAsync(j => j.Name == name, ct);
        if (exists)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Job title with the same name already exists.");

        // Validate ParentJobId if provided
        if (request.ParentJobId.HasValue)
        {
            var parentExists = await _db.JobTitles
                .AnyAsync(j => j.Id == request.ParentJobId.Value, ct);
            if (!parentExists)
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid ParentJobId.");
        }

        var jobTitle = new JobTitle
        {
            Name = name,
            NameAr = request.NameAr?.Trim(),
            JobLevel = request.JobLevel,
            IsManagerRole = request.IsManagerRole,
            ParentJobId = request.ParentJobId
        };

        _db.JobTitles.Add(jobTitle);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetJobTitles), new { id = jobTitle.Id }, jobTitle);
    }
}

public sealed class CreateJobTitleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public int? JobLevel { get; set; }
    public bool IsManagerRole { get; set; }
    public int? ParentJobId { get; set; }
}

