using internalEmployee.Auth.Contracts;
using internalEmployee.Auth.Models;
using internalEmployee.Data;
using internalEmployee.Data.Entities;
using internalEmployee.Services.Auth;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RequestStatus = internalEmployee.Data.Entities.RequestStatus;

namespace internalEmployee.Services.Home;

public sealed class HomeService : IHomeService
{
    private readonly IAuthService _authService;
    private readonly AppDbContext _db;

    public HomeService(IAuthService authService, AppDbContext db)
    {
        _authService = authService;
        _db = db;
    }

    public async Task<HomeResponse> GetHomeAsync(ClaimsPrincipal claimsPrincipal, CancellationToken ct)
    {
        var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User ID not found in token.");

        var user = await _authService.GetUserByIdAsync(userId, ct);
        if (user == null)
            throw new InvalidOperationException("User not found.");
        if (user.Role != AppRole.User)
            throw new UnauthorizedAccessException("Only users with role User can access this endpoint.");

        var greeting = GetGreeting();

        string? departmentName = null;
        if (user.DepartmentId.HasValue)
        {
            var department = await _db.Departments
                .FirstOrDefaultAsync(d => d.Id == user.DepartmentId.Value, ct);
            departmentName = department?.Name;
        }

        string? jobTitleName = null;
        if (user.JobId.HasValue)
        {
            var jobTitle = await _db.JobTitles
                .FirstOrDefaultAsync(j => j.Id == user.JobId.Value, ct);
            jobTitleName = jobTitle?.Name;
        }

        // Get last 3 requests for each status
        var allRequests = await GetLastThreeRequestsByStatusAsync(user.Id, null, ct);
        var pendingRequests = await GetLastThreeRequestsByStatusAsync(user.Id, RequestStatus.Pending, ct);
        var acceptedRequests = await GetLastThreeRequestsByStatusAsync(user.Id, RequestStatus.Approved, ct);
        var rejectedRequests = await GetLastThreeRequestsByStatusAsync(user.Id, RequestStatus.Rejected, ct);

        // Get today's attendance record
        var today = DateOnly.FromDateTime(DateTime.Now);
        var todayAttendance = await _db.Attendances
            .FirstOrDefaultAsync(a => a.UserId == user.Id && a.Date == today, ct);

        return new HomeResponse
        {
            Greeting = greeting,
            FullNameAr = $"{user.FirstNameAr} {user.MiddleNameAr} {user.LastNameAr}".Trim().Replace("  ", " "),
            FullNameEn = $"{user.FirstNameEn} {user.MiddleNameEn} {user.LastNameEn}".Trim().Replace("  ", " "),
            JobTitle = jobTitleName ?? user.JobTitle,
            DepartmentName = departmentName,
            ImageUrl = user.ImageUrl,
            TodayAttendanceTime = todayAttendance?.AttendanceTime,
            TodayDepartureTime = todayAttendance?.DepartureTime,
            AllRequests = allRequests,
            PendingRequests = pendingRequests,
            AcceptedRequests = acceptedRequests,
            RejectedRequests = rejectedRequests
        };
    }

    private async Task<List<RequestItem>> GetLastThreeRequestsByStatusAsync(Guid userId, RequestStatus? status, CancellationToken ct)
    {
        // Fetch last requests from each table with optional status filter
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

    public async Task<List<RequestItem>> GetAllRequestsAsync(ClaimsPrincipal claimsPrincipal, int? month, CancellationToken ct)
    {
        var userId = await GetUserIdFromClaimsAsync(claimsPrincipal, ct);
        return await GetAllRequestsByStatusAsync(userId, null, month, ct);
    }

    public async Task<List<RequestItem>> GetAllPendingRequestsAsync(ClaimsPrincipal claimsPrincipal, int? month, CancellationToken ct)
    {
        var userId = await GetUserIdFromClaimsAsync(claimsPrincipal, ct);
        return await GetAllRequestsByStatusAsync(userId, RequestStatus.Pending, month, ct);
    }

    public async Task<List<RequestItem>> GetAllAcceptedRequestsAsync(ClaimsPrincipal claimsPrincipal, int? month, CancellationToken ct)
    {
        var userId = await GetUserIdFromClaimsAsync(claimsPrincipal, ct);
        return await GetAllRequestsByStatusAsync(userId, RequestStatus.Approved, month, ct);
    }

    public async Task<List<RequestItem>> GetAllRejectedRequestsAsync(ClaimsPrincipal claimsPrincipal, int? month, CancellationToken ct)
    {
        var userId = await GetUserIdFromClaimsAsync(claimsPrincipal, ct);
        return await GetAllRequestsByStatusAsync(userId, RequestStatus.Rejected, month, ct);
    }

    private async Task<Guid> GetUserIdFromClaimsAsync(ClaimsPrincipal claimsPrincipal, CancellationToken ct)
    {
        var userIdClaim = claimsPrincipal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User ID not found in token.");

        return userId;
    }

    private async Task<List<RequestItem>> GetAllRequestsByStatusAsync(Guid userId, RequestStatus? status, int? month, CancellationToken ct)
    {
        // Fetch requests for the specific user with optional status and month filter
        var permissionsQuery = _db.Permissions.Where(p => p.UserId == userId);
        if (status.HasValue)
            permissionsQuery = permissionsQuery.Where(p => p.Status == status.Value);
        if (month.HasValue && month.Value >= 1 && month.Value <= 12)
            permissionsQuery = permissionsQuery.Where(p => p.CreatedAt.Month == month.Value);
        
        var permissions = await permissionsQuery
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        var leavesQuery = _db.Leaves.Where(l => l.UserId == userId);
        if (status.HasValue)
            leavesQuery = leavesQuery.Where(l => l.Status == status.Value);
        if (month.HasValue && month.Value >= 1 && month.Value <= 12)
            leavesQuery = leavesQuery.Where(l => l.CreatedAt.Month == month.Value);
        
        var leaves = await leavesQuery
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);

        var assignmentsQuery = _db.Assignments.Where(a => a.UserId == userId);
        if (status.HasValue)
            assignmentsQuery = assignmentsQuery.Where(a => a.Status == status.Value);
        if (month.HasValue && month.Value >= 1 && month.Value <= 12)
            assignmentsQuery = assignmentsQuery.Where(a => a.CreatedAt.Month == month.Value);
        
        var assignments = await assignmentsQuery
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        var advancesQuery = _db.SalaryAdvances.Where(a => a.UserId == userId);
        if (status.HasValue)
            advancesQuery = advancesQuery.Where(a => a.Status == status.Value);
        if (month.HasValue && month.Value >= 1 && month.Value <= 12)
            advancesQuery = advancesQuery.Where(a => a.CreatedAt.Month == month.Value);

        var advances = await advancesQuery
            .OrderByDescending(a => a.CreatedAt)
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

        // Combine all requests and sort by CreatedAt descending
        var allRequests = permissionItems
            .Concat(leaveItems)
            .Concat(assignmentItems)
            .Concat(advanceItems)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        return allRequests;
    }

    public async Task<HRHomeResponse> GetHRHomeAsync(ClaimsPrincipal claimsPrincipal, CancellationToken ct)
    {
        var nationalIdClaim = claimsPrincipal.FindFirst("nationalId")?.Value;
        if (string.IsNullOrWhiteSpace(nationalIdClaim))
            throw new UnauthorizedAccessException("NationalId not found in token.");

        var hrUser = await _authService.GetUserByNationalIdAsync(nationalIdClaim, ct);
        if (hrUser == null)
            throw new InvalidOperationException("User not found.");

        var greeting = GetGreeting();

        string? departmentName = null;
        if (hrUser.DepartmentId.HasValue)
        {
            var department = await _db.Departments
                .FirstOrDefaultAsync(d => d.Id == hrUser.DepartmentId.Value, ct);
            departmentName = department?.Name;
        }

        string? jobTitleName = null;
        if (hrUser.JobId.HasValue)
        {
            var jobTitle = await _db.JobTitles
                .FirstOrDefaultAsync(j => j.Id == hrUser.JobId.Value, ct);
            jobTitleName = jobTitle?.Name;
        }

        // Get last 3 requests for each status for HR user
        var allRequests = await GetLastThreeRequestsByStatusAsync(hrUser.Id, null, ct);
        var pendingRequests = await GetLastThreeRequestsByStatusAsync(hrUser.Id, RequestStatus.Pending, ct);
        var acceptedRequests = await GetLastThreeRequestsByStatusAsync(hrUser.Id, RequestStatus.Approved, ct);
        var rejectedRequests = await GetLastThreeRequestsByStatusAsync(hrUser.Id, RequestStatus.Rejected, ct);

        // Get today's attendance record for HR user
        var today = DateOnly.FromDateTime(DateTime.Now);
        var todayAttendance = await _db.Attendances
            .FirstOrDefaultAsync(a => a.UserId == hrUser.Id && a.Date == today, ct);

        // Get all Users (Role = User and IsActive = true) from database with Department information
        var users = await _db.Users
            .Where(u => u.Role != AppRole.SuperAdmin  && u.IsActive)
            .OrderBy(u => u.FirstNameEn ?? u.FirstNameAr ?? u.NationalId ?? u.PassportNumber)
            .ToListAsync(ct);

        // Calculate statistics
        var totalEmployees = users.Count;
        
        // Get all departments (including those without employees)
        var allDepartments = await _db.Departments
            .OrderBy(d => d.Name)
            .ToListAsync(ct);
        var totalDepartments = allDepartments.Count;

        // Calculate employees per department
        var employeesPerDepartment = allDepartments.Select(d => new DepartmentEmployeeCount
        {
            DepartmentId = d.Id,
            DepartmentName = d.Name,
            EmployeeCount = users.Count(u => u.DepartmentId.HasValue && u.DepartmentId.Value == d.Id)
        }).ToList();

        // Add entry for employees without department
        var employeesWithoutDepartment = users.Count(u => !u.DepartmentId.HasValue);
        if (employeesWithoutDepartment > 0)
        {
            employeesPerDepartment.Add(new DepartmentEmployeeCount
            {
                DepartmentId = 0,
                DepartmentName = "بدون قسم",
                EmployeeCount = employeesWithoutDepartment
            });
        }

        // Get all departments for mapping
        var departmentIds = users.Where(u => u.DepartmentId.HasValue).Select(u => u.DepartmentId!.Value).Distinct().ToList();
        var departments = await _db.Departments
            .Where(d => departmentIds.Contains(d.Id))
            .ToListAsync(ct);

        var departmentDict = departments.ToDictionary(d => d.Id, d => d.Name);

        // Get all nationalities for mapping
        var nationalityIds = users.Where(u => u.NationalityId.HasValue).Select(u => u.NationalityId!.Value).Distinct().ToList();
        var nationalities = await _db.Nationalities
            .Where(n => nationalityIds.Contains(n.Id))
            .ToListAsync(ct);

        var nationalityDict = nationalities.ToDictionary(n => n.Id, n => n.Name);

        // Map Users to EmployeeItem DTOs
        var jobIds = users.Where(u => u.JobId.HasValue).Select(u => u.JobId!.Value).Distinct().ToList();
        var jobTitles = await _db.JobTitles
            .Where(j => jobIds.Contains(j.Id))
            .ToListAsync(ct);
        var jobTitleDict = jobTitles.ToDictionary(j => j.Id, j => j.Name);

        var employees = users.Select(u => new EmployeeItem
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
            DepartmentId = u.DepartmentId,
            DepartmentName = u.DepartmentId.HasValue && departmentDict.ContainsKey(u.DepartmentId.Value) 
                ? departmentDict[u.DepartmentId.Value] 
                : null,
            JobId = u.JobId,
            JobTitleName = u.JobId.HasValue && jobTitleDict.ContainsKey(u.JobId.Value)
                ? jobTitleDict[u.JobId.Value]
                : null,
            JobTitle = u.JobTitle,
            Role = u.Role.ToString(),
            IsPending = u.IsPending,
            IsMale = u.IsMale,
            StartDate = u.StartDate,
            ImageUrl = u.ImageUrl,
            NationalityId = u.NationalityId,
            NationalityName = u.NationalityId.HasValue && nationalityDict.ContainsKey(u.NationalityId.Value)
                ? nationalityDict[u.NationalityId.Value]
                : null
        }).Take(3).ToList();

        return new HRHomeResponse
        {
            Greeting = greeting,
            FullNameAr = $"{hrUser.FirstNameAr} {hrUser.MiddleNameAr} {hrUser.LastNameAr}".Trim().Replace("  ", " "),
            FullNameEn = $"{hrUser.FirstNameEn} {hrUser.MiddleNameEn} {hrUser.LastNameEn}".Trim().Replace("  ", " "),
            JobTitle = jobTitleName ?? hrUser.JobTitle,
            DepartmentName = departmentName,
            ImageUrl = hrUser.ImageUrl,
            TodayAttendanceTime = todayAttendance?.AttendanceTime,
            TodayDepartureTime = todayAttendance?.DepartureTime,
            AllRequests = allRequests,
            PendingRequests = pendingRequests,
            AcceptedRequests = acceptedRequests,
            RejectedRequests = rejectedRequests,
            Employees = employees,
            Statistics = new HRHomeStatistics
            {
                TotalEmployees = totalEmployees,
                TotalDepartments = totalDepartments,
                EmployeesPerDepartment = employeesPerDepartment
            }
        };
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

