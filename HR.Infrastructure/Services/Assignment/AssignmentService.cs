using internalEmployee.Auth.Contracts;
using internalEmployee.Auth.Models;
using internalEmployee.Data;
using internalEmployee.Data.Entities;
using internalEmployee.Services.Notification;
using Microsoft.EntityFrameworkCore;
using AssignmentEntity = internalEmployee.Data.Entities.Assignment;
using RequestStatus = internalEmployee.Data.Entities.RequestStatus;

namespace internalEmployee.Services.Assignment;

public sealed class AssignmentService : IAssignmentService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;

    public AssignmentService(AppDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<AssignmentEntity> CreateAssignmentAsync(Guid userId, AssignmentRequest request, CancellationToken ct)
    {
        // Check if user exists
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        // Validate dates
        if (request.EndDate < request.StartDate)
            throw new InvalidOperationException("End date must be greater than or equal to start date.");

        // Combine DateOnly with TimeOnly to create DateTime
        var startDate = request.StartDate.ToDateTime(TimeOnly.MinValue);
        var endDate = request.EndDate.ToDateTime(TimeOnly.MinValue);
        var startDateTime = request.StartDate.ToDateTime(request.StartTime);
        var endDateTime = request.EndDate.ToDateTime(request.EndTime);

        var assignment = new AssignmentEntity
        {
            UserId = userId,
            Where = request.Where.Trim(),
            StartDate = startDate,
            EndDate = endDate,
            StartTime = startDateTime,
            EndTime = endDateTime,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            Status = RequestStatus.Pending
        };

        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync(ct);

        // Send notifications to admin, HR, and superadmin
        var notificationMessage = $"طلب مامورية في {request.Where} بتاريخ {request.StartDate:yyyy-MM-dd} من {request.StartTime:HH:mm} إلى {request.EndTime:HH:mm}"
            + (string.IsNullOrWhiteSpace(assignment.Reason) ? string.Empty : $". السبب: {assignment.Reason}");
        await _notificationService.SendRequestNotificationAsync(
            userId,
            NotificationType.Assignment,
            assignment.Id,
            notificationMessage,
            ct);

        return assignment;
    }

    public async Task<List<AssignmentEntity>> GetUserAssignmentsAsync(Guid userId, CancellationToken ct)
    {
        return await _db.Assignments
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<AssignmentEntity>> GetAllAssignmentsAsync(CancellationToken ct)
    {
        return await _db.Assignments
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<PaginatedResponse<AssignmentResponse>> GetAllAssignmentsPaginatedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        RequestStatus? status,
        Guid? userId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken ct)
    {
        // Join Assignments with Users so we can search/filter on employee name
        var query =
            from a in _db.Assignments
            join u in _db.Users on a.UserId equals u.Id into userJoin
            from u in userJoin.DefaultIfEmpty()
            select new { Assignment = a, User = u };

        // ── Search by employee name (AR or EN) or by location (Where) ──
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
                x.Assignment.Where.ToLower().Contains(term) ||
                (x.Assignment.Reason != null && x.Assignment.Reason.ToLower().Contains(term))
            );
        }

        // ── Filter by status ──
        if (status.HasValue)
            query = query.Where(x => x.Assignment.Status == status.Value);

        // ── Filter by specific user ──
        if (userId.HasValue)
            query = query.Where(x => x.Assignment.UserId == userId.Value);

        // ── Filter by date range (matches StartDate of the assignment) ──
        if (dateFrom.HasValue)
        {
            var from = dateFrom.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.Assignment.StartDate >= from);
        }
        if (dateTo.HasValue)
        {
            var to = dateTo.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(x => x.Assignment.StartDate <= to);
        }

        var totalCount = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(x => x.Assignment.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = rows.Select(x =>
        {
            var a = x.Assignment;
            var u = x.User;
            var nameAr = string.Join(" ", new[] { u?.FirstNameAr, u?.MiddleNameAr, u?.LastNameAr }
                .Where(n => !string.IsNullOrWhiteSpace(n)));
            var nameEn = string.Join(" ", new[] { u?.FirstNameEn, u?.MiddleNameEn, u?.LastNameEn }
                .Where(n => !string.IsNullOrWhiteSpace(n)));

            return new AssignmentResponse
            {
                Id             = a.Id,
                UserId         = a.UserId,
                EmployeeNameAr = string.IsNullOrWhiteSpace(nameAr) ? null : nameAr,
                EmployeeNameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn,
                Where          = a.Where,
                StartDate      = DateOnly.FromDateTime(a.StartTime),
                EndDate        = DateOnly.FromDateTime(a.EndTime),
                StartTime      = TimeOnly.FromDateTime(a.StartTime),
                EndTime        = TimeOnly.FromDateTime(a.EndTime),
                Reason         = a.Reason,
                CreatedAt      = a.CreatedAt,
                Status         = a.Status.ToString(),
                RejectionReason = a.RejectionReason
            };
        }).ToList();

        return new PaginatedResponse<AssignmentResponse>
        {
            Items      = items,
            PageNumber = pageNumber,
            PageSize   = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PaginatedResponse<AssignmentResponse>> GetDepartmentAssignmentsPaginatedAsync(
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
            throw new InvalidOperationException("Only Admin or SuperAdmin can access department assignments.");

        if (!currentUser.DepartmentId.HasValue)
        {
            return new PaginatedResponse<AssignmentResponse>
            {
                Items = new List<AssignmentResponse>(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = 0
            };
        }

        var query =
            from a in _db.Assignments
            join u in _db.Users on a.UserId equals u.Id
            where u.DepartmentId == currentUser.DepartmentId.Value && u.Role == AppRole.User
            select new { Assignment = a, User = u };

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
                x.Assignment.Where.ToLower().Contains(term) ||
                (x.Assignment.Reason != null && x.Assignment.Reason.ToLower().Contains(term)));
        }

        if (status.HasValue)
            query = query.Where(x => x.Assignment.Status == status.Value);

        if (userId.HasValue)
            query = query.Where(x => x.Assignment.UserId == userId.Value);

        if (dateFrom.HasValue)
        {
            var from = dateFrom.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.Assignment.StartDate >= from);
        }

        if (dateTo.HasValue)
        {
            var to = dateTo.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(x => x.Assignment.StartDate <= to);
        }

        var totalCount = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(x => x.Assignment.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = rows.Select(x =>
        {
            var a = x.Assignment;
            var u = x.User;
            var nameAr = string.Join(" ", new[] { u.FirstNameAr, u.MiddleNameAr, u.LastNameAr }
                .Where(n => !string.IsNullOrWhiteSpace(n)));
            var nameEn = string.Join(" ", new[] { u.FirstNameEn, u.MiddleNameEn, u.LastNameEn }
                .Where(n => !string.IsNullOrWhiteSpace(n)));

            return new AssignmentResponse
            {
                Id = a.Id,
                UserId = a.UserId,
                EmployeeNameAr = string.IsNullOrWhiteSpace(nameAr) ? null : nameAr,
                EmployeeNameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn,
                Where = a.Where,
                StartDate = DateOnly.FromDateTime(a.StartTime),
                EndDate = DateOnly.FromDateTime(a.EndTime),
                StartTime = TimeOnly.FromDateTime(a.StartTime),
                EndTime = TimeOnly.FromDateTime(a.EndTime),
                Reason = a.Reason,
                CreatedAt = a.CreatedAt,
                Status = a.Status.ToString(),
                RejectionReason = a.RejectionReason
            };
        }).ToList();

        return new PaginatedResponse<AssignmentResponse>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }


    public async Task<AssignmentEntity> UpdateStatusAsync(int assignmentId, Guid currentUserId, UpdateStatusRequest request, CancellationToken ct)
    {
        var assignment = await _db.Assignments.FindAsync(new object[] { assignmentId }, ct);
        if (assignment == null)
            throw new InvalidOperationException("Assignment not found.");

        // Check authorization: Only SuperAdmin or Admin of the same department can update status
        var currentUser = await _db.Users.FindAsync(new object[] { currentUserId }, ct);
        if (currentUser == null)
            throw new InvalidOperationException("Current user not found.");

        var requestUser = await _db.Users.FindAsync(new object[] { assignment.UserId }, ct);
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

        var oldStatus = assignment.Status;
        assignment.Status = request.Status;
        assignment.RejectionReason = request.Status == RequestStatus.Rejected 
            ? request.RejectionReason?.Trim() 
            : null;

        await _db.SaveChangesAsync(ct);

        // Send notification to user if status changed
        if (oldStatus != request.Status && request.Status != RequestStatus.Pending)
        {
            await _notificationService.SendStatusChangeNotificationAsync(
                assignment.UserId,
                NotificationType.Assignment,
                assignment.Id,
                request.Status,
                assignment.RejectionReason,
                ct);
        }

        return assignment;
    }

    public async Task SendReminderAsync(int assignmentId, Guid currentUserId, CancellationToken ct)
    {
        var assignment = await _db.Assignments.FindAsync(new object[] { assignmentId }, ct);
        if (assignment == null)
            throw new InvalidOperationException("Assignment not found.");

        if (assignment.UserId != currentUserId)
            throw new InvalidOperationException("You can only remind your own assignment request.");

        if (assignment.Status != RequestStatus.Pending)
            throw new InvalidOperationException("Assignment request is not pending.");

        var requestMessage = $"تذكير بطلب مامورية إلى {assignment.Where} من {assignment.StartTime:yyyy-MM-dd HH:mm} إلى {assignment.EndTime:yyyy-MM-dd HH:mm}"
            + (string.IsNullOrWhiteSpace(assignment.Reason) ? string.Empty : $". السبب: {assignment.Reason}");

        await _notificationService.SendRequestReminderAsync(
            assignment.UserId,
            NotificationType.Assignment,
            assignment.Id,
            requestMessage,
            ct);
    }
}

