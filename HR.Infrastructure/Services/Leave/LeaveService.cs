using internalEmployee.Auth.Contracts;
using internalEmployee.Auth.Models;
using internalEmployee.Data;
using internalEmployee.Data.Entities;
using internalEmployee.Services.EmployeeHistory;
using internalEmployee.Services.Notification;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using LeaveEntity = internalEmployee.Data.Entities.Leave;
using RequestStatus = internalEmployee.Data.Entities.RequestStatus;

namespace internalEmployee.Services.Leave;

public sealed class LeaveService : ILeaveService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IWebHostEnvironment _environment;
    private readonly IEmployeeHistoryService _historyService;
    private readonly ILogger<LeaveService> _logger;
    private const long MaxMedicalReportSize = 10 * 1024 * 1024; // 10 MB
    private static readonly string[] AllowedMedicalReportExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };

    public LeaveService(
        AppDbContext db, 
        INotificationService notificationService, 
        IWebHostEnvironment environment, 
        IEmployeeHistoryService historyService,
        ILogger<LeaveService> logger)
    {
        _db = db;
        _notificationService = notificationService;
        _environment = environment;
        _historyService = historyService;
        _logger = logger;
    }

    public async Task<LeaveEntity> CreateLeaveAsync(Guid userId, LeaveRequest request, IFormFile? medicalReport, Guid? doneByUserId, CancellationToken ct)
    {
        // Check if user exists
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        // Validate dates
        if (request.EndDate < request.StartDate)
            throw new InvalidOperationException("End date must be greater than or equal to start date.");

        var hasOverlappingLeaveRequest = await HasOverlappingLeaveAsync(
            userId,
            request.StartDate,
            request.EndDate,
            new[] { RequestStatus.Pending, RequestStatus.Approved },
            excludeLeaveId: null,
            ct);

        if (hasOverlappingLeaveRequest)
            throw new InvalidOperationException(
                $"يوجد بالفعل طلب إجازة مسجل لنفس الموظف خلال الفترة من {request.StartDate:yyyy-MM-dd} إلى {request.EndDate:yyyy-MM-dd} أو خلال فترة متداخلة معها، لذلك لا يمكن إرسال طلب جديد.");

        var requestedDays = CalculateWorkingDays(request.StartDate, request.EndDate);

        // Validate sick leave requires medical report
        if (request.LeaveType == LeaveType.Sick && (medicalReport == null || medicalReport.Length == 0))
            throw new InvalidOperationException("Medical report is required for sick leave.");

        // Validate casual leave rules
        if (request.LeaveType == LeaveType.Casual)
        {
            // Check if casual leave exceeds 6 days per year
            var currentYear = DateTime.Now.Year;
            var yearStart = new DateOnly(currentYear, 1, 1);
            var yearEnd = new DateOnly(currentYear, 12, 31);
            
            var casualLeavesThisYear = await _db.Leaves
                .Where(l => l.UserId == userId 
                    && l.LeaveType == LeaveType.Casual 
                    && (l.Status == RequestStatus.Approved || l.Status == RequestStatus.Pending)
                    && DateOnly.FromDateTime(l.StartDate) >= yearStart
                    && DateOnly.FromDateTime(l.StartDate) <= yearEnd)
                .ToListAsync(ct);

            var totalCasualDays = casualLeavesThisYear.Sum(l => CalculateWorkingDays(
                DateOnly.FromDateTime(l.StartDate), 
                DateOnly.FromDateTime(l.EndDate)));

            if (totalCasualDays + requestedDays > 6)
                throw new InvalidOperationException($"Casual leave cannot exceed 6 days per year. You have used/pending {totalCasualDays} days this year.");

            // Check if casual leave exceeds remaining annual leave balance
            var balance = await GetLeaveBalanceAsync(userId, ct);
            if (requestedDays > balance.AnnualLeaveRemaining)
                throw new InvalidOperationException($"Casual leave cannot exceed remaining annual leave balance ({balance.AnnualLeaveRemaining} days).");
        }

        // Validate annual leave balance
        if (request.LeaveType == LeaveType.Annual)
        {
            var balance = await GetLeaveBalanceAsync(userId, ct);
            if (requestedDays > balance.AnnualLeaveRemaining)
                throw new InvalidOperationException($"ليس لديك رصيد إجازات سنوية كافٍ. الرصيد المتاح: {balance.AnnualLeaveRemaining} يوم، والمطلوب: {requestedDays} يوم.");
        }

        // Sick leave has no balance limit

        // Validate special leave rules
        if (request.LeaveType == LeaveType.Hajj)
        {
            // Check if user has 5 years of service
            if (!user.StartDate.HasValue)
                throw new InvalidOperationException("StartDate is required for Hajj leave.");

            var yearsOfService = (DateTime.Now - user.StartDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays / 365.25;
            if (yearsOfService < 5)
                throw new InvalidOperationException("Hajj leave requires at least 5 years of service.");

            // Check if user already used Hajj leave
            var hasHajjLeave = await _db.Leaves
                .AnyAsync(l => l.UserId == userId && l.LeaveType == LeaveType.Hajj && l.Status == RequestStatus.Approved, ct);
            if (hasHajjLeave)
                throw new InvalidOperationException("Hajj leave can only be used once during service.");
        }

        if (request.LeaveType == LeaveType.Maternity)
        {
            // Check if user has used maternity leave more than 3 times
            var maternityCount = await _db.Leaves
                .CountAsync(l => l.UserId == userId && l.LeaveType == LeaveType.Maternity && l.Status == RequestStatus.Approved, ct);
            if (maternityCount >= 3)
                throw new InvalidOperationException("Maternity leave can only be used up to 3 times.");
        }

        // Handle medical report upload
        string? medicalReportUrl = null;
        if (medicalReport != null && medicalReport.Length > 0)
        {
            // Validate file size
            if (medicalReport.Length > MaxMedicalReportSize)
                throw new InvalidOperationException($"Medical report exceeds maximum size of {MaxMedicalReportSize / (1024 * 1024)} MB.");

            // Validate file extension
            var fileExtension = Path.GetExtension(medicalReport.FileName).ToLowerInvariant();
            if (!AllowedMedicalReportExtensions.Contains(fileExtension))
                throw new InvalidOperationException($"Invalid medical report format. Allowed formats: {string.Join(", ", AllowedMedicalReportExtensions)}");

            // Create uploads directory if it doesn't exist
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "medical-reports");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Generate unique filename
            var fileName = $"{userId}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Save file
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await medicalReport.CopyToAsync(stream, ct);
            }

            medicalReportUrl = $"/uploads/medical-reports/{fileName}";
        }

        // Convert DateOnly to DateTime (at midnight)
        var leave = new LeaveEntity
        {
            UserId = userId,
            StartDate = request.StartDate.ToDateTime(TimeOnly.MinValue),
            EndDate = request.EndDate.ToDateTime(TimeOnly.MinValue),
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            Status = RequestStatus.Pending,
            LeaveType = request.LeaveType,
            MedicalReportUrl = medicalReportUrl
        };

        _db.Leaves.Add(leave);
        await _db.SaveChangesAsync(ct);

        var historyPayload = new
        {
            LeaveId = leave.Id,
            LeaveType = request.LeaveType.ToString(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Days = requestedDays,
            Reason = leave.Reason,
            Status = leave.Status.ToString()
        };

        await _historyService.CreateHistoryAsync(
            new CreateEmployeeHistoryRequest
            {
                EmployeeId = userId,
                EventType = EmployeeEventType.LeaveRequest,
                NewValue = JsonSerializer.Serialize(historyPayload),
                Reason = leave.Reason,
                Notes = "Leave request created"
            },
            doneByUserId,
            ct);

        _logger.LogInformation(
            "Leave request created. LeaveId={LeaveId} UserId={UserId} Type={LeaveType} Start={StartDate} End={EndDate} Days={Days} HasMedicalReport={HasMedicalReport}",
            leave.Id,
            userId,
            request.LeaveType,
            request.StartDate,
            request.EndDate,
            requestedDays,
            medicalReport != null && medicalReport.Length > 0);

        // Send notifications to admin, HR, and superadmin
        var leaveTypeName = request.LeaveType switch
        {
            LeaveType.Annual => "سنوية",
            LeaveType.Casual => "عارضية",
            LeaveType.Sick => "مرضية",
            LeaveType.Maternity => "وضع",
            LeaveType.Paternity => "أبوة",
            LeaveType.Hajj => "حج",
            LeaveType.Exam => "امتحانات",
            _ => "إجازة"
        };
        var notificationMessage = $"طلب إجازة {leaveTypeName} من {request.StartDate:yyyy-MM-dd} إلى {request.EndDate:yyyy-MM-dd}"
            + (string.IsNullOrWhiteSpace(leave.Reason) ? string.Empty : $". السبب: {leave.Reason}");
        await _notificationService.SendRequestNotificationAsync(
            userId,
            NotificationType.Leave,
            leave.Id,
            notificationMessage,
            ct);

        return leave;
    }

    public async Task<LeaveBalanceResponse> GetLeaveBalanceAsync(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        if (!user.StartDate.HasValue)
            throw new InvalidOperationException("StartDate is required to calculate leave balance.");

        // Calculate eligible annual leave balance
        var annualLeaveBalance = CalculateEligibleAnnualLeave(user.StartDate.Value, user.Birthday, user.IsDisabled);

        // Get all approved leaves
        var approvedLeaves = await _db.Leaves
            .Where(l => l.UserId == userId && l.Status == RequestStatus.Approved)
            .ToListAsync(ct);

        // Calculate used days
        var annualLeaveUsed = 0m;
        var casualLeaveUsed = 0m;
        var sickLeaveUsed = 0m;
        var maternity = 0;
        var paternity = 0;
        var hajj = 0;
        var exam = 0;

        foreach (var leave in approvedLeaves)
        {
            var days = CalculateWorkingDays(
                DateOnly.FromDateTime(leave.StartDate),
                DateOnly.FromDateTime(leave.EndDate));

            switch (leave.LeaveType)
            {
                case LeaveType.Annual:
                    annualLeaveUsed += days;
                    break;
                case LeaveType.Casual:
                    casualLeaveUsed += days;
                    annualLeaveUsed += days; // Casual is deducted from annual
                    break;
                case LeaveType.Sick:
                    sickLeaveUsed += days;
                    break;
                case LeaveType.Maternity:
                    maternity++;
                    break;
                case LeaveType.Paternity:
                    paternity++;
                    break;
                case LeaveType.Hajj:
                    hajj++;
                    break;
                case LeaveType.Exam:
                    exam++;
                    break;
            }
        }

        // Calculate remaining annual leave
        var annualLeaveRemaining = Math.Max(0, annualLeaveBalance - annualLeaveUsed);

        // Sick leave balance - use manual balance set by HR, or 0 if not set
        var sickLeaveBalance = user.SickLeaveBalance ?? 0m;

        return new LeaveBalanceResponse
        {
            AnnualLeaveBalance = annualLeaveBalance,
            AnnualLeaveUsed = annualLeaveUsed,
            AnnualLeaveRemaining = annualLeaveRemaining,
            CasualLeaveUsed = casualLeaveUsed,
            SickLeaveBalance = sickLeaveBalance,
            SickLeaveUsed = sickLeaveUsed,
            Maternity = maternity,
            Paternity = paternity,
            Hajj = hajj,
            Exam = exam
        };
    }

    private decimal CalculateEligibleAnnualLeave(DateOnly startDate, DateOnly? birthday, bool isDisabled)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var monthsOfService = (today.Year - startDate.Year) * 12 + (today.Month - startDate.Month);
        if (today.Day < startDate.Day)
            monthsOfService--;

        var yearsOfService = monthsOfService / 12.0;

        // Disabled employees keep existing policy
        if (isDisabled)
            return 45m;

        var age = birthday.HasValue
            ? (today.Year - birthday.Value.Year - (today.DayOfYear < birthday.Value.DayOfYear ? 1 : 0))
            : 0;

        // Age >= 50: 50 days
        if (age >= 50)
            return 50m;

        // 10 years of service: 30 days
        if (yearsOfService >= 10)
            return 30m;

        // First 6 months: 0 days
        if (monthsOfService < 6)
            return 0m;

        // From 6 months to < 1 year: 15 days
        if (monthsOfService < 12)
            return 15m;

        // From 1 year to < 10 years: 21 days
        return 21m;
    }

    private decimal CalculateWorkingDays(DateOnly startDate, DateOnly endDate)
    {
        var days = 0m;
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            // Exclude Friday (5) and Saturday (6)
            if (currentDate.DayOfWeek != DayOfWeek.Friday && currentDate.DayOfWeek != DayOfWeek.Saturday)
            {
                days++;
            }
            currentDate = currentDate.AddDays(1);
        }

        return days;
    }

    private async Task<bool> HasOverlappingLeaveAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        IEnumerable<RequestStatus> statuses,
        int? excludeLeaveId,
        CancellationToken ct)
    {
        var start = startDate.ToDateTime(TimeOnly.MinValue);
        var end = endDate.ToDateTime(TimeOnly.MinValue);
        var allowedStatuses = statuses.ToArray();

        return await _db.Leaves.AnyAsync(l =>
            l.UserId == userId &&
            allowedStatuses.Contains(l.Status) &&
            (!excludeLeaveId.HasValue || l.Id != excludeLeaveId.Value) &&
            l.StartDate <= end &&
            l.EndDate >= start,
            ct);
    }

    public async Task<List<LeaveEntity>> GetUserLeavesAsync(Guid userId, CancellationToken ct)
    {
        // Verify user exists
        var userExists = await _db.Users.AnyAsync(u => u.Id == userId, ct);
        if (!userExists)
            throw new InvalidOperationException("User not found.");

        var leaves = await _db.Leaves
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);

        return leaves;
    }

    public async Task<List<LeaveEntity>> GetAllPendingLeavesAsync(CancellationToken ct)
    {
        return await _db.Leaves
            .Where(l => l.Status == RequestStatus.Pending)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<PaginatedResponse<LeaveResponse>> GetDepartmentLeavesPaginatedAsync(
        Guid currentUserId,
        int pageNumber,
        int pageSize,
        string? search,
        RequestStatus? status,
        Guid? userId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken ct)
    {
        var currentUser = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUserId, ct);

        if (currentUser == null)
            throw new InvalidOperationException("Current user not found.");

        if (currentUser.Role != AppRole.Admin && currentUser.Role != AppRole.SuperAdmin)
            throw new InvalidOperationException("Only Admin or SuperAdmin can access department leaves.");

        if (!currentUser.DepartmentId.HasValue)
        {
            return new PaginatedResponse<LeaveResponse>
            {
                Items = new List<LeaveResponse>(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = 0
            };
        }

        var query =
            from l in _db.Leaves
            join u in _db.Users on l.UserId equals u.Id
            where u.DepartmentId == currentUser.DepartmentId.Value && u.Role == AppRole.User
            select new { Leave = l, User = u };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                (x.User.FirstNameAr != null && x.User.FirstNameAr.ToLower().Contains(term)) ||
                (x.User.MiddleNameAr != null && x.User.MiddleNameAr.ToLower().Contains(term)) ||
                (x.User.LastNameAr != null && x.User.LastNameAr.ToLower().Contains(term)) ||
                (x.User.FirstNameEn != null && x.User.FirstNameEn.ToLower().Contains(term)) ||
                (x.User.MiddleNameEn != null && x.User.MiddleNameEn.ToLower().Contains(term)) ||
                (x.User.LastNameEn != null && x.User.LastNameEn.ToLower().Contains(term)) ||
                (x.User.EmployeeCode != null && x.User.EmployeeCode.ToLower().Contains(term)) ||
                (x.Leave.Reason != null && x.Leave.Reason.ToLower().Contains(term)));
        }

        if (status.HasValue)
            query = query.Where(x => x.Leave.Status == status.Value);

        if (userId.HasValue)
            query = query.Where(x => x.Leave.UserId == userId.Value);

        if (dateFrom.HasValue)
        {
            var from = dateFrom.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.Leave.StartDate >= from);
        }

        if (dateTo.HasValue)
        {
            var to = dateTo.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(x => x.Leave.StartDate <= to);
        }

        var totalCount = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(x => x.Leave.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = rows.Select(x =>
        {
            var nameAr = string.Join(" ", new[] { x.User.FirstNameAr, x.User.MiddleNameAr, x.User.LastNameAr }
                .Where(n => !string.IsNullOrWhiteSpace(n)));
            var nameEn = string.Join(" ", new[] { x.User.FirstNameEn, x.User.MiddleNameEn, x.User.LastNameEn }
                .Where(n => !string.IsNullOrWhiteSpace(n)));

            return new LeaveResponse
            {
                Id = x.Leave.Id,
                UserId = x.Leave.UserId,
                EmployeeNameAr = string.IsNullOrWhiteSpace(nameAr) ? null : nameAr,
                EmployeeNameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn,
                StartDate = DateOnly.FromDateTime(x.Leave.StartDate),
                EndDate = DateOnly.FromDateTime(x.Leave.EndDate),
                Reason = x.Leave.Reason,
                CreatedAt = x.Leave.CreatedAt,
                Status = x.Leave.Status.ToString(),
                RejectionReason = x.Leave.RejectionReason,
                LeaveType = x.Leave.LeaveType.ToString(),
                MedicalReportUrl = x.Leave.MedicalReportUrl
            };
        }).ToList();

        return new PaginatedResponse<LeaveResponse>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<LeaveEntity> UpdateStatusAsync(int leaveId, Guid currentUserId, UpdateStatusRequest request, CancellationToken ct)
    {
        var leave = await _db.Leaves.FindAsync(new object[] { leaveId }, ct);
        if (leave == null)
            throw new InvalidOperationException("Leave not found.");

        // Check authorization: Only SuperAdmin or Admin of the same department can update status
        var currentUser = await _db.Users.FindAsync(new object[] { currentUserId }, ct);
        if (currentUser == null)
            throw new InvalidOperationException("Current user not found.");

        var requestUser = await _db.Users.FindAsync(new object[] { leave.UserId }, ct);
        if (requestUser == null)
            throw new InvalidOperationException("Request user not found.");

        // Check if current user is SuperAdmin
        if (currentUser.Role != AppRole.SuperAdmin)
        {
            // If not SuperAdmin, must be Admin (Department Manager) in the same department
            if (currentUser.Role == AppRole.Admin)
            {
                // Admin can only update requests from users in the same department
                if (currentUser.DepartmentId == null || requestUser.DepartmentId == null || 
                    currentUser.DepartmentId != requestUser.DepartmentId)
                    throw new InvalidOperationException("You can only update requests from users in your department.");
            }
            else
            {
                throw new InvalidOperationException("Only SuperAdmin or Department Manager can update request status.");
            }
        }

        var oldStatus = leave.Status;

        // Validate leave balance when approving
        if (request.Status == RequestStatus.Approved && oldStatus != RequestStatus.Approved)
        {
            var hasOtherApprovedOverlap = await HasOverlappingLeaveAsync(
                leave.UserId,
                DateOnly.FromDateTime(leave.StartDate),
                DateOnly.FromDateTime(leave.EndDate),
                new[] { RequestStatus.Approved },
                leave.Id,
                ct);

            if (hasOtherApprovedOverlap)
                throw new InvalidOperationException(
                    $"لا يمكن الموافقة على طلب الإجازة للفترة من {leave.StartDate:yyyy-MM-dd} إلى {leave.EndDate:yyyy-MM-dd} لأن هناك إجازة أخرى معتمدة بالفعل لنفس الموظف في نفس الفترة أو في فترة متداخلة معها.");

            // Calculate working days (excluding Friday and Saturday)
            var days = CalculateWorkingDays(
                DateOnly.FromDateTime(leave.StartDate),
                DateOnly.FromDateTime(leave.EndDate));

            // Get leave balance
            if (!requestUser.StartDate.HasValue)
                throw new InvalidOperationException("Employee StartDate is required to validate leave balance.");

            var balance = await GetLeaveBalanceAsync(leave.UserId, ct);

            // Validate based on leave type
            switch (leave.LeaveType)
            {
                case LeaveType.Annual:
                    if (balance.AnnualLeaveRemaining < days)
                        throw new InvalidOperationException($"Insufficient annual leave balance. Available: {balance.AnnualLeaveRemaining} days, Requested: {days} days.");
                    break;

                case LeaveType.Casual:
                    // Check if casual leave exceeds 6 days per year
                    var currentYear = DateTime.Now.Year;
                    var yearStart = new DateOnly(currentYear, 1, 1);
                    var yearEnd = new DateOnly(currentYear, 12, 31);
                    
                    var casualLeavesThisYear = await _db.Leaves
                        .Where(l => l.UserId == leave.UserId 
                            && l.LeaveType == LeaveType.Casual 
                            && l.Status == RequestStatus.Approved
                            && l.Id != leave.Id // Exclude current leave
                            && DateOnly.FromDateTime(l.StartDate) >= yearStart
                            && DateOnly.FromDateTime(l.StartDate) <= yearEnd)
                        .ToListAsync(ct);

                    var totalCasualDays = casualLeavesThisYear.Sum(l => CalculateWorkingDays(
                        DateOnly.FromDateTime(l.StartDate), 
                        DateOnly.FromDateTime(l.EndDate)));

                    if (totalCasualDays + days > 6)
                        throw new InvalidOperationException($"Casual leave cannot exceed 6 days per year. Already used: {totalCasualDays} days, Requested: {days} days.");

                    // Check annual leave balance
                    if (balance.AnnualLeaveRemaining < days)
                        throw new InvalidOperationException($"Insufficient annual leave balance for casual leave. Available: {balance.AnnualLeaveRemaining} days, Requested: {days} days.");
                    break;

                case LeaveType.Sick:
                    // Sick leave has no balance limit
                    break;

                case LeaveType.Maternity:
                case LeaveType.Paternity:
                case LeaveType.Hajj:
                case LeaveType.Exam:
                    // Special leaves don't require balance check
                    break;

                default:
                    throw new InvalidOperationException($"Unknown leave type: {leave.LeaveType}");
            }
        }

        leave.Status = request.Status;
        leave.RejectionReason = request.Status == RequestStatus.Rejected 
            ? request.RejectionReason?.Trim() 
            : null;

        await _db.SaveChangesAsync(ct);

        // Send notification to user if status changed
        if (oldStatus != request.Status && request.Status != RequestStatus.Pending)
        {
            await _notificationService.SendStatusChangeNotificationAsync(
                leave.UserId,
                NotificationType.Leave,
                leave.Id,
                request.Status,
                leave.RejectionReason,
                ct);
        }

        return leave;
    }

    public async Task SendReminderAsync(int leaveId, Guid currentUserId, CancellationToken ct)
    {
        var leave = await _db.Leaves.FindAsync(new object[] { leaveId }, ct);
        if (leave == null)
            throw new InvalidOperationException("Leave not found.");

        if (leave.UserId != currentUserId)
            throw new InvalidOperationException("You can only remind your own leave request.");

        if (leave.Status != RequestStatus.Pending)
            throw new InvalidOperationException("Leave request is not pending.");

        var requestMessage = $"تذكير بطلب إجازة من {leave.StartDate:yyyy-MM-dd} إلى {leave.EndDate:yyyy-MM-dd}"
            + (string.IsNullOrWhiteSpace(leave.Reason) ? string.Empty : $". السبب: {leave.Reason}");

        await _notificationService.SendRequestReminderAsync(
            leave.UserId,
            NotificationType.Leave,
            leave.Id,
            requestMessage,
            ct);
    }

    public async Task<PaginatedEmployeeLeaveBalanceResponse> GetAllEmployeesWithLeaveBalanceAsync(
        string? search, 
        Guid? userId, 
        int pageNumber, 
        int pageSize, 
        CancellationToken ct)
    {
        // Start with all active employees
        var query = _db.Users
            .AsNoTracking()
            .Where(u => u.IsActive);

        // Apply UserId filter
        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            query = query.Where(u => u.Id == userId.Value);
        }

        // Apply Search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim();
            query = query.Where(u => 
                (u.FirstNameAr != null && u.FirstNameAr.Contains(searchTerm)) ||
                (u.MiddleNameAr != null && u.MiddleNameAr.Contains(searchTerm)) ||
                (u.LastNameAr != null && u.LastNameAr.Contains(searchTerm)) ||
                (u.FirstNameEn != null && u.FirstNameEn.Contains(searchTerm)) ||
                (u.MiddleNameEn != null && u.MiddleNameEn.Contains(searchTerm)) ||
                (u.LastNameEn != null && u.LastNameEn.Contains(searchTerm)) ||
                (u.EmployeeCode != null && u.EmployeeCode.Contains(searchTerm)) ||
                (u.Email != null && u.Email.Contains(searchTerm)));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(ct);

        // Apply Pagination
        var employees = await query
            .OrderBy(u => u.FirstNameAr ?? u.FirstNameEn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = new List<EmployeeLeaveBalanceResponse>();

        foreach (var employee in employees)
        {
            // Get employee name (prefer Arabic, then English)
            var employeeName = !string.IsNullOrWhiteSpace(employee.FirstNameAr)
                ? $"{employee.FirstNameAr} {employee.MiddleNameAr} {employee.LastNameAr}".Trim()
                : !string.IsNullOrWhiteSpace(employee.FirstNameEn)
                    ? $"{employee.FirstNameEn} {employee.MiddleNameEn} {employee.LastNameEn}".Trim()
                    : null;

            // Get leave balance
            LeaveBalanceResponse leaveBalance;
            try
            {
                if (employee.StartDate.HasValue)
                {
                    leaveBalance = await GetLeaveBalanceAsync(employee.Id, ct);
                }
                else
                {
                    // If no StartDate, return empty balance
                    leaveBalance = new LeaveBalanceResponse();
                }
            }
            catch
            {
                // If error calculating balance, return empty balance
                leaveBalance = new LeaveBalanceResponse();
            }

            items.Add(new EmployeeLeaveBalanceResponse
            {
                EmployeeId = employee.Id,
                EmployeeName = employeeName,
                LeaveBalance = leaveBalance
            });
        }

        return new PaginatedEmployeeLeaveBalanceResponse
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task UpdateSickLeaveBalanceAsync(Guid employeeId, decimal balance, DateOnly startDate, DateOnly endDate, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == employeeId, ct);
        if (user == null)
            throw new InvalidOperationException("Employee not found.");

        if (balance < 0)
            throw new InvalidOperationException("Sick leave balance cannot be negative.");

        if (startDate > endDate)
            throw new InvalidOperationException("Start date must be before or equal to end date.");

        user.SickLeaveBalance = balance;
        await _db.SaveChangesAsync(ct);
    }
}
