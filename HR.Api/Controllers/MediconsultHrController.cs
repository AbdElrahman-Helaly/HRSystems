using internalEmployee.Data;
using internalEmployee.Services.MediconsultHr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/{lang}/MediconsultHR")]
[Authorize]
public sealed class MediconsultHrController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMediconsultHrService _mediconsultHrService;

    public MediconsultHrController(AppDbContext db, IMediconsultHrService mediconsultHrService)
    {
        _db = db;
        _mediconsultHrService = mediconsultHrService;
    }

    public enum KpiMode
    {
        daily = 0,
        weekly = 1,
        monthly = 2
    }

    public sealed class KpiModeItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public sealed class UserMachineCodeItem
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MachineCode { get; set; }
    }

    [HttpGet("/api/MediconsultHR/modes")]
    [ProducesResponseType(typeof(List<KpiModeItem>), StatusCodes.Status200OK)]
    public ActionResult<List<KpiModeItem>> GetAllModes()
    {
        var modes = Enum.GetValues<KpiMode>()
            .Select(m => new KpiModeItem
            {
                Id = (int)m,
                Name = m.ToString()
            })
            .ToList();

        return Ok(modes);
    }

    [HttpGet("/api/MediconsultHR/users")]
    [ProducesResponseType(typeof(List<UserMachineCodeItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserMachineCodeItem>>> GetAllUsers(CancellationToken ct)
    {
        var users = await _db.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FirstNameEn ?? u.FirstNameAr ?? u.NationalId ?? u.PassportNumber)
            .Select(u => new UserMachineCodeItem
            {
                FirstName = u.FirstNameEn ?? u.FirstNameAr,
                LastName = u.LastNameEn ?? u.LastNameAr,
                MachineCode = u.MachineCode
            })
            .ToListAsync(ct);

        return Ok(users);
    }

    [HttpGet("GetApprovalsKPIs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApprovalsKpis(
        [FromRoute] string lang,
        [FromQuery] string? fingerPrintId,
        [FromQuery] int? mode,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        CancellationToken ct,
        [FromQuery] int? pageNumber = 1,
        [FromQuery] int? pageSize = 10)
    {
        if (mode.HasValue && (mode.Value < 0 || mode.Value > 2))
            return BadRequest("mode must be 0, 1, or 2.");

        if (pageNumber.HasValue && pageNumber.Value <= 0)
            return BadRequest("pageNumber must be greater than 0.");

        if (pageSize.HasValue && pageSize.Value <= 0)
            return BadRequest("pageSize must be greater than 0.");

        if (!string.IsNullOrWhiteSpace(fingerPrintId))
        {
            var exists = await _db.Users.AnyAsync(u => u.MachineCode == fingerPrintId, ct);
            if (!exists)
                return NotFound();
        }

        try
        {
            var (statusCode, content) = await _mediconsultHrService.GetApprovalsKpisAsync(
                lang,
                fingerPrintId,
                mode,
                dateFrom,
                dateTo,
                pageNumber,
                pageSize,
                ct);

            return new ContentResult
            {
                StatusCode = (int)statusCode,
                Content = content,
                ContentType = "application/json"
            };
        }
        catch (Exception ex)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Failed to fetch Mediconsult data.",
                detail: ex.Message);
        }
    }
}
