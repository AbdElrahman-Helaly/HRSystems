using internalEmployee.Auth.Contracts;
using internalEmployee.Auth.Models;
using internalEmployee.Data;
using internalEmployee.Data.Entities;
using internalEmployee.Services.Auth;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace internalEmployee.Services.SuperAdminDashboard;

public sealed class SuperAdminDashboardService : ISuperAdminDashboardService
{
    private readonly IAuthService _authService;
    private readonly AppDbContext _db;

    public SuperAdminDashboardService(IAuthService authService, AppDbContext db)
    {
        _authService = authService;
        _db = db;
    }

    public async Task<SuperAdminDashboardResponse> GetSuperAdminDashboardAsync(ClaimsPrincipal claimsPrincipal, CancellationToken ct)
    {
        var userIdClaim = claimsPrincipal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User ID not found in token.");

        var superAdmin = await _authService.GetUserByIdAsync(userId, ct);
        if (superAdmin == null)
            throw new InvalidOperationException("User not found.");

        // Check if user is SuperAdmin
        if (superAdmin.Role != AppRole.SuperAdmin)
            throw new UnauthorizedAccessException("Access denied. SuperAdmin role required.");

        var greeting = GetGreeting();

        // Get today's date for attendance lookup
        var today = DateOnly.FromDateTime(DateTime.Now);

        // Get today's attendance record for SuperAdmin
        var todayAttendance = await _db.Attendances
            .FirstOrDefaultAsync(a => a.UserId == superAdmin.Id && a.Date == today, ct);

        // Get all departments
        var departments = await _db.Departments
            .OrderBy(d => d.Name)
            .ToListAsync(ct);

        // Get all employees (Role = User) from all departments
        var allEmployees = await _db.Users
            .Where(u => u.Role == AppRole.User)
            .ToListAsync(ct);

        // Get all attendance records for today for all employees
        var allEmployeeIds = allEmployees.Select(u => u.Id).ToList();
        var todayAttendances = await _db.Attendances
            .Where(a => allEmployeeIds.Contains(a.UserId) && a.Date == today)
            .ToListAsync(ct);

        // Create a dictionary for quick lookup
        var attendanceDict = todayAttendances.ToDictionary(a => a.UserId);

        AdminUserItem MapDashboardEmployee(AppUser user)
        {
            var attendance = attendanceDict.GetValueOrDefault(user.Id);
            return new AdminUserItem
            {
                Id = user.Id,
                FirstNameAr = user.FirstNameAr,
                MiddleNameAr = user.MiddleNameAr,
                LastNameAr = user.LastNameAr,
                FirstNameEn = user.FirstNameEn,
                MiddleNameEn = user.MiddleNameEn,
                LastNameEn = user.LastNameEn,
                FullNameAr = BuildFullName(user.FirstNameAr, user.MiddleNameAr, user.LastNameAr),
                FullNameEn = BuildFullName(user.FirstNameEn, user.MiddleNameEn, user.LastNameEn),
                IsActive = user.IsActive,
                ImageUrl = user.ImageUrl,
                TodayAttendanceTime = attendance?.AttendanceTime,
                TodayDepartureTime = attendance?.DepartureTime
            };
        }

        // Build department dashboard items
        var departmentItems = new List<DepartmentDashboardItem>();

        foreach (var department in departments)
        {
            // Get employees in this department
            var departmentEmployees = allEmployees
                .Where(u => u.DepartmentId.HasValue && u.DepartmentId.Value == department.Id)
                .OrderBy(u => u.FirstNameEn ?? u.FirstNameAr ?? u.NationalId ?? u.PassportNumber)
                .ToList();

            var departmentEmployeeIds = departmentEmployees.Select(u => u.Id).ToList();

            // Get employees with attendance times
            var employeeItems = departmentEmployees
                .Select(MapDashboardEmployee)
                .ToList();

            // Get all requests from department employees
            var departmentRequests = await GetAllRequestsByUserIdsAsync(departmentEmployeeIds, ct);

            departmentItems.Add(new DepartmentDashboardItem
            {
                DepartmentId = department.Id,
                DepartmentName = department.Name,
                Employees = employeeItems,
                Requests = departmentRequests
            });
        }

        // Handle employees without department
        var employeesWithoutDepartment = allEmployees
            .Where(u => !u.DepartmentId.HasValue)
            .OrderBy(u => u.FirstNameEn ?? u.FirstNameAr ?? u.NationalId ?? u.PassportNumber)
            .ToList();

        if (employeesWithoutDepartment.Any())
        {
            var employeesWithoutDepartmentIds = employeesWithoutDepartment.Select(u => u.Id).ToList();

            var employeeItems = employeesWithoutDepartment
                .Select(MapDashboardEmployee)
                .ToList();

            var requests = await GetAllRequestsByUserIdsAsync(employeesWithoutDepartmentIds, ct);

            departmentItems.Add(new DepartmentDashboardItem
            {
                DepartmentId = 0,
                DepartmentName = "بدون قسم",
                Employees = employeeItems,
                Requests = requests
            });
        }

        return new SuperAdminDashboardResponse
        {
            Greeting = greeting,
            FullNameAr = BuildFullName(superAdmin.FirstNameAr, superAdmin.MiddleNameAr, superAdmin.LastNameAr),
            FullNameEn = BuildFullName(superAdmin.FirstNameEn, superAdmin.MiddleNameEn, superAdmin.LastNameEn),
            JobTitle = superAdmin.JobTitle,
            ImageUrl = superAdmin.ImageUrl,
            TodayAttendanceTime = todayAttendance?.AttendanceTime,
            TodayDepartureTime = todayAttendance?.DepartureTime,
            Departments = departmentItems
        };
    }

    private async Task<List<RequestItem>> GetAllRequestsByUserIdsAsync(List<Guid> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0)
            return new List<RequestItem>();

        // Fetch all requests from each table (no status filter, no limit)
        var permissions = await _db.Permissions
            .Where(p => userIds.Contains(p.UserId))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        var leaves = await _db.Leaves
            .Where(l => userIds.Contains(l.UserId))
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);

        var assignments = await _db.Assignments
            .Where(a => userIds.Contains(a.UserId))
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        var advances = await _db.SalaryAdvances
            .Where(a => userIds.Contains(a.UserId))
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        // Convert to RequestItem with DateOnly/TimeOnly
        var permissionItems = permissions.Select(p => new RequestItem
        {
            Id = p.Id,
            Type = "Permission",
            CreatedAt = p.CreatedAt,
            Status = p.Status.ToString(),
            UserId = p.UserId,
            Date = DateOnly.FromDateTime(p.Date),
            StartTime = TimeOnly.FromDateTime(p.StartTime),
            EndTime = TimeOnly.FromDateTime(p.EndTime),
            Reason = p.Reason
        });

        var leaveItems = leaves.Select(l => new RequestItem
        {
            Id = l.Id,
            Type = "Leave",
            CreatedAt = l.CreatedAt,
            Status = l.Status.ToString(),
            UserId = l.UserId,
            StartDate = DateOnly.FromDateTime(l.StartDate),
            EndDate = DateOnly.FromDateTime(l.EndDate),
            Reason = l.Reason
        });

        var assignmentItems = assignments.Select(a => new RequestItem
        {
            Id = a.Id,
            Type = "Assignment",
            CreatedAt = a.CreatedAt,
            Status = a.Status.ToString(),
            UserId = a.UserId,
            StartDate = DateOnly.FromDateTime(a.StartTime),
            StartTime = TimeOnly.FromDateTime(a.StartTime),
            EndTime = TimeOnly.FromDateTime(a.EndTime),
            Where = a.Where,
            Reason = a.Reason
        });

        var advanceItems = advances.Select(a => new RequestItem
        {
            Id = a.Id,
            Type = "Advance",
            CreatedAt = a.CreatedAt,
            Status = a.Status.ToString(),
            UserId = a.UserId,
            StartDate = DateOnly.FromDateTime(a.StartDate),
            Reason = a.Reason,
            Amount = a.Amount,
            MonthlyDeduction = a.MonthlyDeduction
        });

        // Combine all requests and sort by CreatedAt descending
        var allRequests = permissionItems
            .Concat(leaveItems)
            .Concat(assignmentItems)
            .Concat(advanceItems)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        return allRequests;
    }

    private static string? BuildFullName(string? firstName, string? middleName, string? lastName)
    {
        var fullName = $"{firstName} {middleName} {lastName}".Trim().Replace("  ", " ");
        return string.IsNullOrWhiteSpace(fullName) ? null : fullName;
    }

    private static string GetGreeting()
    {
        // Get current time in Egypt timezone (Africa/Cairo)
        TimeZoneInfo? egyptTimeZone = null;
        
        // Try Windows timezone ID first
        try
        {
            egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        }
        catch
        {
            // Try Linux/Mac timezone ID
            try
            {
                egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
            }
            catch
            {
                // Fallback: use UTC+2 (Egypt standard time)
                egyptTimeZone = TimeZoneInfo.CreateCustomTimeZone("Egypt", TimeSpan.FromHours(2), "Egypt", "Egypt");
            }
        }

        var egyptTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);
        var hour = egyptTime.Hour;

        if (hour < 12)
            return "Good morning";
        else if (hour < 18)
            return "Good afternoon";
        else
            return "Good evening";
    }
}
