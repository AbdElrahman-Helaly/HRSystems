using internalEmployee.Auth.Contracts;
using internalEmployee.Auth.Models;
using internalEmployee.Data;
using internalEmployee.Data.Entities;
using internalEmployee.Services.Auth;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RequestStatus = internalEmployee.Data.Entities.RequestStatus;

namespace internalEmployee.Services.AdminDashboard;

public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly IAuthService _authService;
    private readonly AppDbContext _db;

    public AdminDashboardService(IAuthService authService, AppDbContext db)
    {
        _authService = authService;
        _db = db;
    }

    public async Task<AdminDashboardResponse> GetAdminDashboardAsync(ClaimsPrincipal claimsPrincipal, CancellationToken ct)
    {
        var userIdClaim = claimsPrincipal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User ID not found in token.");

        var admin = await _authService.GetUserByIdAsync(userId, ct);
        if (admin == null)
            throw new InvalidOperationException("User not found.");

        // Check if user is Admin or SuperAdmin
        if (admin.Role != AppRole.Admin && admin.Role != AppRole.SuperAdmin)
            throw new UnauthorizedAccessException("Access denied. Admin role required.");

        // If admin doesn't have a department, return empty list
        if (!admin.DepartmentId.HasValue)
        {
            return new AdminDashboardResponse
            {
                Greeting = GetGreeting(),
                FullNameAr = $"{admin.FirstNameAr} {admin.MiddleNameAr} {admin.LastNameAr}".Trim().Replace("  ", " "),
                FullNameEn = $"{admin.FirstNameEn} {admin.MiddleNameEn} {admin.LastNameEn}".Trim().Replace("  ", " "),
                JobTitle = admin.JobTitle,
                DepartmentName = null,
                ImageUrl = admin.ImageUrl,
                TodayAttendanceTime = null,
                TodayDepartureTime = null,
                AllRequests = new List<RequestItem>(),
                PendingRequests = new List<RequestItem>(),
                AcceptedRequests = new List<RequestItem>(),
                RejectedRequests = new List<RequestItem>(),
                Employees = new List<EmployeeItem>()
            };
        }

        var greeting = GetGreeting();

        string? departmentName = null;
        var department = await _db.Departments
            .FirstOrDefaultAsync(d => d.Id == admin.DepartmentId.Value, ct);
        departmentName = department?.Name;

        // Get admin requests with same grouping used on user homepage
        var allRequests = await GetLastThreeRequestsByStatusAsync(admin.Id, null, ct);
        var pendingRequests = await GetLastThreeRequestsByStatusAsync(admin.Id, RequestStatus.Pending, ct);
        var acceptedRequests = await GetLastThreeRequestsByStatusAsync(admin.Id, RequestStatus.Approved, ct);
        var rejectedRequests = await GetLastThreeRequestsByStatusAsync(admin.Id, RequestStatus.Rejected, ct);

        // Get all employees (Role = User) in the same department
        var departmentUsers = await _db.Users
            .Where(u => u.DepartmentId == admin.DepartmentId.Value && u.Role == AppRole.User)
            .OrderBy(u => u.FirstNameEn ?? u.FirstNameAr ?? u.NationalId ?? u.PassportNumber)
            .ToListAsync(ct);

        // Get today's date for attendance lookup
        var today = DateOnly.FromDateTime(DateTime.Now);
        
        // Get all attendance records for today for department users
        var departmentUserIds = departmentUsers.Select(u => u.Id).ToList();
        var todayAttendances = await _db.Attendances
            .Where(a => departmentUserIds.Contains(a.UserId) && a.Date == today)
            .ToListAsync(ct);

        // Create a dictionary for quick lookup
        var attendanceDict = todayAttendances.ToDictionary(a => a.UserId);

        var branchIds = departmentUsers.Where(u => u.BranchId.HasValue).Select(u => u.BranchId!.Value).Distinct().ToList();
        var branches = await _db.Branches
            .AsNoTracking()
            .Where(b => branchIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.Name, ct);

        var jobIds = departmentUsers.Where(u => u.JobId.HasValue).Select(u => u.JobId!.Value).Distinct().ToList();
        var jobTitles = await _db.JobTitles
            .AsNoTracking()
            .Where(j => jobIds.Contains(j.Id))
            .ToDictionaryAsync(j => j.Id, j => j.Name, ct);

        var managerIds = departmentUsers.Where(u => u.ManagerId.HasValue).Select(u => u.ManagerId!.Value).Distinct().ToList();
        var managers = await _db.Users
            .AsNoTracking()
            .Where(u => managerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => new { u.FirstNameAr, u.MiddleNameAr, u.LastNameAr, u.FirstNameEn, u.MiddleNameEn, u.LastNameEn }, ct);

        var employmentModeIds = departmentUsers.Where(u => u.EmploymentModeId.HasValue).Select(u => u.EmploymentModeId!.Value).Distinct().ToList();
        var employmentModes = await _db.EmploymentModes
            .AsNoTracking()
            .Where(e => employmentModeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Name, ct);

        var employees = departmentUsers.Select(u =>
        {
            string? managerName = null;
            if (u.ManagerId.HasValue && managers.TryGetValue(u.ManagerId.Value, out var manager))
            {
                var managerNameAr = $"{manager.FirstNameAr} {manager.MiddleNameAr} {manager.LastNameAr}".Trim().Replace("  ", " ");
                var managerNameEn = $"{manager.FirstNameEn} {manager.MiddleNameEn} {manager.LastNameEn}".Trim().Replace("  ", " ");
                managerName = !string.IsNullOrWhiteSpace(managerNameAr) ? managerNameAr : managerNameEn;
            }

            return new EmployeeItem
            {
                Id = u.Id,
                NationalId = u.NationalId,
                PassportNumber = u.PassportNumber,
                FirstNameAr = u.FirstNameAr,
                MiddleNameAr = u.MiddleNameAr,
                LastNameAr = u.LastNameAr,
                FirstNameEn = u.FirstNameEn,
                MiddleNameEn = u.MiddleNameEn,
                LastNameEn = u.LastNameEn,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                EmployeeCode = u.EmployeeCode,
                BranchId = u.BranchId,
                BranchName = u.BranchId.HasValue && branches.TryGetValue(u.BranchId.Value, out var branchName) ? branchName : null,
                JobId = u.JobId,
                JobTitleName = u.JobId.HasValue && jobTitles.TryGetValue(u.JobId.Value, out var jobTitleName) ? jobTitleName : null,
                ManagerId = u.ManagerId,
                ManagerName = managerName,
                MaritalStatusId = u.MaritalStatusId,
                AddressAr = u.AddressAr,
                AddressEn = u.AddressEn,
                EmploymentModeId = u.EmploymentModeId,
                EmploymentModeName = u.EmploymentModeId.HasValue && employmentModes.TryGetValue(u.EmploymentModeId.Value, out var employmentModeName) ? employmentModeName : null,
                GovernorateId = u.GovernorateId,
                CityId = u.CityId,
                IsActive = u.IsActive,
                DepartmentId = u.DepartmentId,
                DepartmentName = departmentName,
                JobTitle = u.JobTitle,
                Role = u.Role.ToString(),
                IsPending = u.IsPending,
                IsMale = u.IsMale,
                StartDate = u.StartDate,
                ImageUrl = u.ImageUrl,
                NationalityId = u.NationalityId
            };
        }).ToList();

        // Get today's attendance record for admin
        var todayAttendance = await _db.Attendances
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == admin.Id && a.Date == today, ct);

        return new AdminDashboardResponse
        {
            Greeting = greeting,
            FullNameAr = $"{admin.FirstNameAr} {admin.MiddleNameAr} {admin.LastNameAr}".Trim().Replace("  ", " "),
            FullNameEn = $"{admin.FirstNameEn} {admin.MiddleNameEn} {admin.LastNameEn}".Trim().Replace("  ", " "),
            JobTitle = admin.JobTitle,
            DepartmentName = departmentName,
            ImageUrl = admin.ImageUrl,
            TodayAttendanceTime = todayAttendance?.AttendanceTime,
            TodayDepartureTime = todayAttendance?.DepartureTime,
            AllRequests = allRequests,
            PendingRequests = pendingRequests,
            AcceptedRequests = acceptedRequests,
            RejectedRequests = rejectedRequests,
            Employees = employees
        };
    }

    private async Task<List<RequestItem>> GetLastThreeRequestsByStatusAsync(Guid userId, RequestStatus? status, CancellationToken ct)
    {
        var permissionsQuery = _db.Permissions.Where(p => p.UserId == userId);
        if (status.HasValue)
            permissionsQuery = permissionsQuery.Where(p => p.Status == status.Value);

        var permissions = await permissionsQuery
            .OrderByDescending(p => p.CreatedAt)
            .Take(3)
            .ToListAsync(ct);

        var leavesQuery = _db.Leaves.Where(l => l.UserId == userId);
        if (status.HasValue)
            leavesQuery = leavesQuery.Where(l => l.Status == status.Value);

        var leaves = await leavesQuery
            .OrderByDescending(l => l.CreatedAt)
            .Take(3)
            .ToListAsync(ct);

        var assignmentsQuery = _db.Assignments.Where(a => a.UserId == userId);
        if (status.HasValue)
            assignmentsQuery = assignmentsQuery.Where(a => a.Status == status.Value);

        var assignments = await assignmentsQuery
            .OrderByDescending(a => a.CreatedAt)
            .Take(3)
            .ToListAsync(ct);

        var advancesQuery = _db.SalaryAdvances.Where(a => a.UserId == userId);
        if (status.HasValue)
            advancesQuery = advancesQuery.Where(a => a.Status == status.Value);

        var advances = await advancesQuery
            .OrderByDescending(a => a.CreatedAt)
            .Take(3)
            .ToListAsync(ct);

        // Convert to RequestItem with DateOnly/TimeOnly
        var permissionItems = permissions.Select(p => new RequestItem
        {
            Id = p.Id,
            Type = "Permission",
            CreatedAt = p.CreatedAt,
            Status = p.Status.ToString(),
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
            StartDate = DateOnly.FromDateTime(a.StartDate),
            Reason = a.Reason,
            Amount = a.Amount,
            MonthlyDeduction = a.MonthlyDeduction
        });

        // Combine all requests, sort by CreatedAt descending, and take top 3
        var allRequests = permissionItems
            .Concat(leaveItems)
            .Concat(assignmentItems)
            .Concat(advanceItems)
            .OrderByDescending(r => r.CreatedAt)
            .Take(3)
            .ToList();

        return allRequests;
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

