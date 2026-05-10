using internalEmployee.Data;
using internalEmployee.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class InsuranceSettingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public InsuranceSettingsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(InsuranceSettings), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InsuranceSettings>> GetActiveSettings(CancellationToken ct)
    {
        var settings = await _db.InsuranceSettings
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (settings == null)
            return NotFound();

        return Ok(settings);
    }

    [HttpGet("all")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(List<InsuranceSettings>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<InsuranceSettings>>> GetAllSettings(CancellationToken ct)
    {
        var settings = await _db.InsuranceSettings
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

        return Ok(settings);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(InsuranceSettings), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InsuranceSettings>> CreateSettings([FromBody] CreateInsuranceSettingsRequest request, CancellationToken ct)
    {
        if (request.EmployeePercentage < 0 || request.EmployeePercentage > 100)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "EmployeePercentage must be between 0 and 100.");

        if (request.CompanyPercentage < 0 || request.CompanyPercentage > 100)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "CompanyPercentage must be between 0 and 100.");

        if (request.MinimumAmount < 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "MinimumAmount must be greater than or equal to 0.");

        if (request.MaximumAmount <= request.MinimumAmount)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "MaximumAmount must be greater than MinimumAmount.");

        // Deactivate all existing active settings
        var activeSettings = await _db.InsuranceSettings
            .Where(s => s.IsActive)
            .ToListAsync(ct);

        foreach (var setting in activeSettings)
        {
            setting.IsActive = false;
            setting.UpdatedAt = DateTime.Now;
        }

        var newSettings = new InsuranceSettings
        {
            EmployeePercentage = request.EmployeePercentage,
            CompanyPercentage = request.CompanyPercentage,
            MinimumAmount = request.MinimumAmount,
            MaximumAmount = request.MaximumAmount,
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _db.InsuranceSettings.Add(newSettings);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetActiveSettings), new { id = newSettings.Id }, newSettings);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(InsuranceSettings), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InsuranceSettings>> UpdateSettings(int id, [FromBody] UpdateInsuranceSettingsRequest request, CancellationToken ct)
    {
        var settings = await _db.InsuranceSettings.FindAsync(new object[] { id }, ct);
        if (settings == null)
            return NotFound();

        if (request.EmployeePercentage.HasValue)
        {
            if (request.EmployeePercentage.Value < 0 || request.EmployeePercentage.Value > 100)
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "EmployeePercentage must be between 0 and 100.");
            settings.EmployeePercentage = request.EmployeePercentage.Value;
        }

        if (request.CompanyPercentage.HasValue)
        {
            if (request.CompanyPercentage.Value < 0 || request.CompanyPercentage.Value > 100)
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "CompanyPercentage must be between 0 and 100.");
            settings.CompanyPercentage = request.CompanyPercentage.Value;
        }

        if (request.MinimumAmount.HasValue)
        {
            if (request.MinimumAmount.Value < 0)
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "MinimumAmount must be greater than or equal to 0.");
            settings.MinimumAmount = request.MinimumAmount.Value;
        }

        if (request.MaximumAmount.HasValue)
        {
            if (request.MaximumAmount.Value <= settings.MinimumAmount)
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "MaximumAmount must be greater than MinimumAmount.");
            settings.MaximumAmount = request.MaximumAmount.Value;
        }

        if (request.IsActive.HasValue)
        {
            settings.IsActive = request.IsActive.Value;
            
            // If activating, deactivate all other active settings
            if (request.IsActive.Value)
            {
                var otherActiveSettings = await _db.InsuranceSettings
                    .Where(s => s.IsActive && s.Id != id)
                    .ToListAsync(ct);

                foreach (var otherSetting in otherActiveSettings)
                {
                    otherSetting.IsActive = false;
                    otherSetting.UpdatedAt = DateTime.Now;
                }
            }
        }

        settings.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync(ct);

        return Ok(settings);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteSettings(int id, CancellationToken ct)
    {
        var settings = await _db.InsuranceSettings.FindAsync(new object[] { id }, ct);
        if (settings == null)
            return NotFound();

        _db.InsuranceSettings.Remove(settings);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}

public sealed class CreateInsuranceSettingsRequest
{
    public decimal EmployeePercentage { get; set; }
    public decimal CompanyPercentage { get; set; }
    public decimal MinimumAmount { get; set; }
    public decimal MaximumAmount { get; set; }
}

public sealed class UpdateInsuranceSettingsRequest
{
    public decimal? EmployeePercentage { get; set; }
    public decimal? CompanyPercentage { get; set; }
    public decimal? MinimumAmount { get; set; }
    public decimal? MaximumAmount { get; set; }
    public bool? IsActive { get; set; }
}
