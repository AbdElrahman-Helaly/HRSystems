using internalEmployee.Auth.Contracts;
using internalEmployee.Auth.Models;
using internalEmployee.Data;
using internalEmployee.Data.Entities;
using Microsoft.EntityFrameworkCore;
using PublicHolidayEntity = internalEmployee.Data.Entities.PublicHoliday;
using PublicHolidayExceptionEntity = internalEmployee.Data.Entities.PublicHolidayException;

namespace internalEmployee.Services.PublicHoliday;

public sealed class PublicHolidayService : IPublicHolidayService
{
    private readonly AppDbContext _db;

    public PublicHolidayService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<PublicHolidayResponse>> GetAllAsync(int? year, CancellationToken ct)
    {
        var query = _db.PublicHolidays
            .Include(h => h.Exceptions)
                .ThenInclude(e => e.Employee)
            .Include(h => h.Exceptions)
                .ThenInclude(e => e.Department)
            .Include(h => h.Exceptions)
                .ThenInclude(e => e.EmploymentMode)
            .AsQueryable();

        if (year.HasValue)
        {
            query = query.Where(h => h.Year == year.Value);
        }

        var holidays = await query
            .OrderBy(h => h.Date)
            .ToListAsync(ct);

        return holidays.Select(h => new PublicHolidayResponse
        {
            Id = h.Id,
            Date = h.Date,
            Name = h.Name,
            NameAr = h.NameAr,
            Year = h.Year,
            IsActive = h.IsActive,
            Exceptions = h.Exceptions.Select(e => new PublicHolidayExceptionResponse
            {
                Id = e.Id,
                PublicHolidayId = e.PublicHolidayId,
                EmployeeId = e.EmployeeId,
                EmployeeName = e.Employee != null
                    ? $"{e.Employee.FirstNameEn ?? e.Employee.FirstNameAr} {e.Employee.MiddleNameEn ?? e.Employee.MiddleNameAr} {e.Employee.LastNameEn ?? e.Employee.LastNameAr}".Trim()
                    : null,
                DepartmentId = e.DepartmentId,
                DepartmentName = e.Department?.Name,
                EmploymentModeId = e.EmploymentModeId,
                EmploymentModeName = e.EmploymentMode?.Name,
                Religion = e.Religion,
                ReligionName = e.Religion?.ToString()
            }).ToList()
        }).ToList();
    }

    public async Task<PublicHolidayResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var holiday = await _db.PublicHolidays
            .Include(h => h.Exceptions)
                .ThenInclude(e => e.Employee)
            .Include(h => h.Exceptions)
                .ThenInclude(e => e.Department)
            .Include(h => h.Exceptions)
                .ThenInclude(e => e.EmploymentMode)
            .FirstOrDefaultAsync(h => h.Id == id, ct);

        if (holiday == null)
            return null;

        return new PublicHolidayResponse
        {
            Id = holiday.Id,
            Date = holiday.Date,
            Name = holiday.Name,
            NameAr = holiday.NameAr,
            Year = holiday.Year,
            IsActive = holiday.IsActive,
            Exceptions = holiday.Exceptions.Select(e => new PublicHolidayExceptionResponse
            {
                Id = e.Id,
                PublicHolidayId = e.PublicHolidayId,
                EmployeeId = e.EmployeeId,
                EmployeeName = e.Employee != null
                    ? $"{e.Employee.FirstNameEn ?? e.Employee.FirstNameAr} {e.Employee.MiddleNameEn ?? e.Employee.MiddleNameAr} {e.Employee.LastNameEn ?? e.Employee.LastNameAr}".Trim()
                    : null,
                DepartmentId = e.DepartmentId,
                DepartmentName = e.Department?.Name,
                EmploymentModeId = e.EmploymentModeId,
                EmploymentModeName = e.EmploymentMode?.Name,
                Religion = e.Religion,
                ReligionName = e.Religion?.ToString()
            }).ToList()
        };
    }

    public async Task<PublicHolidayEntity> CreateAsync(CreatePublicHolidayRequest request, CancellationToken ct)
    {
        // Check if holiday already exists for this date and year
        var existing = await _db.PublicHolidays
            .FirstOrDefaultAsync(h => h.Date == request.Date && h.Year == request.Year, ct);

        if (existing != null)
            throw new InvalidOperationException($"Public holiday already exists for date {request.Date:yyyy-MM-dd} in year {request.Year}.");

        var holiday = new PublicHolidayEntity
        {
            Date = request.Date,
            Name = request.Name,
            NameAr = request.NameAr,
            Year = request.Year,
            IsActive = request.IsActive
        };

        _db.PublicHolidays.Add(holiday);
        await _db.SaveChangesAsync(ct);

        return holiday;
    }

    public async Task<PublicHolidayEntity> UpdateAsync(int id, UpdatePublicHolidayRequest request, CancellationToken ct)
    {
        var holiday = await _db.PublicHolidays
            .FirstOrDefaultAsync(h => h.Id == id, ct);

        if (holiday == null)
            throw new InvalidOperationException("Public holiday not found.");

        // Check if another holiday exists for this date and year (excluding current)
        var existing = await _db.PublicHolidays
            .FirstOrDefaultAsync(h => h.Date == request.Date && h.Year == request.Year && h.Id != id, ct);

        if (existing != null)
            throw new InvalidOperationException($"Public holiday already exists for date {request.Date:yyyy-MM-dd} in year {request.Year}.");

        holiday.Date = request.Date;
        holiday.Name = request.Name;
        holiday.NameAr = request.NameAr;
        holiday.Year = request.Year;
        holiday.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);

        return holiday;
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var holiday = await _db.PublicHolidays
            .FirstOrDefaultAsync(h => h.Id == id, ct);

        if (holiday == null)
            throw new InvalidOperationException("Public holiday not found.");

        _db.PublicHolidays.Remove(holiday);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PublicHolidayExceptionEntity> AddExceptionAsync(int holidayId, CreatePublicHolidayExceptionRequest request, CancellationToken ct)
    {
        var holiday = await _db.PublicHolidays
            .FirstOrDefaultAsync(h => h.Id == holidayId, ct);

        if (holiday == null)
            throw new InvalidOperationException("Public holiday not found.");

        // Validate that at least one exception type is provided
        if (!request.EmployeeId.HasValue && !request.DepartmentId.HasValue && !request.EmploymentModeId.HasValue && !request.Religion.HasValue)
            throw new InvalidOperationException("At least one exception type (Employee, Department, EmploymentMode, or Religion) must be provided.");

        // Validate that only one exception type is provided
        var exceptionCount = 0;
        if (request.EmployeeId.HasValue) exceptionCount++;
        if (request.DepartmentId.HasValue) exceptionCount++;
        if (request.EmploymentModeId.HasValue) exceptionCount++;
        if (request.Religion.HasValue) exceptionCount++;

        if (exceptionCount > 1)
            throw new InvalidOperationException("Only one exception type (Employee, Department, EmploymentMode, or Religion) can be specified per exception.");

        // Check if exception already exists
        var existing = await _db.PublicHolidayExceptions
            .FirstOrDefaultAsync(e => e.PublicHolidayId == holidayId &&
                ((request.EmployeeId.HasValue && e.EmployeeId == request.EmployeeId) ||
                 (request.DepartmentId.HasValue && e.DepartmentId == request.DepartmentId) ||
                 (request.EmploymentModeId.HasValue && e.EmploymentModeId == request.EmploymentModeId) ||
                 (request.Religion.HasValue && e.Religion == request.Religion)), ct);

        if (existing != null)
            throw new InvalidOperationException("Exception already exists for this holiday.");

        // Validate references exist
        if (request.EmployeeId.HasValue)
        {
            var employee = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.EmployeeId.Value, ct);
            if (employee == null)
                throw new InvalidOperationException("Employee not found.");
        }

        if (request.DepartmentId.HasValue)
        {
            var department = await _db.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId.Value, ct);
            if (department == null)
                throw new InvalidOperationException("Department not found.");
        }

        if (request.EmploymentModeId.HasValue)
        {
            var employmentMode = await _db.EmploymentModes.FirstOrDefaultAsync(e => e.Id == request.EmploymentModeId.Value, ct);
            if (employmentMode == null)
                throw new InvalidOperationException("Employment mode not found.");
        }

        if (request.Religion.HasValue && !Enum.IsDefined(typeof(Religion), request.Religion.Value))
            throw new InvalidOperationException("Religion not found.");

        var exception = new PublicHolidayExceptionEntity
        {
            PublicHolidayId = holidayId,
            EmployeeId = request.EmployeeId,
            DepartmentId = request.DepartmentId,
            EmploymentModeId = request.EmploymentModeId,
            Religion = request.Religion
        };

        _db.PublicHolidayExceptions.Add(exception);
        await _db.SaveChangesAsync(ct);

        return exception;
    }

    public async Task<List<PublicHolidayExceptionResponse>> GetExceptionsAsync(int holidayId, CancellationToken ct)
    {
        var exceptions = await _db.PublicHolidayExceptions
            .Include(e => e.Employee)
            .Include(e => e.Department)
            .Include(e => e.EmploymentMode)
            .Where(e => e.PublicHolidayId == holidayId)
            .ToListAsync(ct);

        return exceptions.Select(e => new PublicHolidayExceptionResponse
        {
            Id = e.Id,
            PublicHolidayId = e.PublicHolidayId,
            EmployeeId = e.EmployeeId,
            EmployeeName = e.Employee != null
                ? $"{e.Employee.FirstNameEn ?? e.Employee.FirstNameAr} {e.Employee.MiddleNameEn ?? e.Employee.MiddleNameAr} {e.Employee.LastNameEn ?? e.Employee.LastNameAr}".Trim()
                : null,
            DepartmentId = e.DepartmentId,
            DepartmentName = e.Department?.Name,
            EmploymentModeId = e.EmploymentModeId,
            EmploymentModeName = e.EmploymentMode?.Name,
            Religion = e.Religion,
            ReligionName = e.Religion?.ToString()
        }).ToList();
    }

    public async Task DeleteExceptionAsync(int exceptionId, CancellationToken ct)
    {
        var exception = await _db.PublicHolidayExceptions
            .FirstOrDefaultAsync(e => e.Id == exceptionId, ct);

        if (exception == null)
            throw new InvalidOperationException("Exception not found.");

        _db.PublicHolidayExceptions.Remove(exception);
        await _db.SaveChangesAsync(ct);
    }
}
