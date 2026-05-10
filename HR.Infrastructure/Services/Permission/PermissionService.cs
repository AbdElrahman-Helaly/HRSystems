using internalEmployee.Auth.Contracts;
using internalEmployee.Auth.Models;
using internalEmployee.Data;
using internalEmployee.Data.Entities;
using internalEmployee.Services.Notification;
using Microsoft.EntityFrameworkCore;
using PermissionEntity = internalEmployee.Data.Entities.Permission;
using RequestStatus = internalEmployee.Data.Entities.RequestStatus;

namespace internalEmployee.Services.Permission;

public sealed class PermissionService : IPermissionService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;

    public PermissionService(AppDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<PermissionEntity> CreatePermissionAsync(Guid userId, PermissionRequest request, CancellationToken ct)
    {
        // Check if user exists
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        // Combine DateOnly with TimeOnly to create DateTime
        var startDateTime = request.Date.ToDateTime(request.StartTime);
        var endDateTime = request.Date.ToDateTime(request.EndTime);

        // Validate permission duration (minimum 1 hour, maximum 4 hours)
        var duration = endDateTime - startDateTime;
        if (duration.TotalHours < 1)
            throw new InvalidOperationException("مدة الإذن لا يمكن أن تقل عن ساعة واحدة.");
        
        if (duration.TotalHours > 4)
            throw new InvalidOperationException("مدة الإذن لا يمكن أن تتجاوز 4 ساعات.");

        // Prevent duplicate/overlapping permissions for the same day
        var targetDate = request.Date.ToDateTime(TimeOnly.MinValue);
        var overlappingPermission = await _db.Permissions
            .AnyAsync(p => p.UserId == userId 
                && (p.Status == RequestStatus.Approved || p.Status == RequestStatus.Pending)
                && p.Date == targetDate
                && p.StartTime < endDateTime 
                && p.EndTime > startDateTime, ct);

        if (overlappingPermission)
            throw new InvalidOperationException("يوجد إذن آخر (مقبول أو معلق) يتعارض مع نفس الوقت في هذا اليوم.");

        // Validate monthly limit (maximum 4 hours total per payroll period: 26th to 25th)
        DateOnly periodStart, periodEnd;
        if (request.Date.Day >= 26)
        {
            periodStart = new DateOnly(request.Date.Year, request.Date.Month, 26);
            var nextMonthDate = request.Date.ToDateTime(TimeOnly.MinValue).AddMonths(1);
            periodEnd = new DateOnly(nextMonthDate.Year, nextMonthDate.Month, 25);
        }
        else
        {
            var prevMonthDate = request.Date.ToDateTime(TimeOnly.MinValue).AddMonths(-1);
            periodStart = new DateOnly(prevMonthDate.Year, prevMonthDate.Month, 26);
            periodEnd = new DateOnly(request.Date.Year, request.Date.Month, 25);
        }

        var permissionsThisPeriod = await _db.Permissions
            .Where(p => p.UserId == userId 
                && (p.Status == RequestStatus.Approved || p.Status == RequestStatus.Pending)
                && DateOnly.FromDateTime(p.Date) >= periodStart
                && DateOnly.FromDateTime(p.Date) <= periodEnd)
            .ToListAsync(ct);

        // Calculate total hours used this period
        var totalHoursThisPeriod = permissionsThisPeriod.Sum(p => (p.EndTime - p.StartTime).TotalHours);
        
        if (totalHoursThisPeriod + duration.TotalHours > 4)
            throw new InvalidOperationException($"لا يمكنك الحصول على أكثر من 4 ساعات إذن في فترة الراتب (من {periodStart:dd/MM} إلى {periodEnd:dd/MM}). لقد استخدمت {totalHoursThisPeriod:F2} ساعة، والمطلوب {duration.TotalHours:F2} ساعة.");

        var permission = new PermissionEntity
        {
            UserId = userId,
            Date = request.Date.ToDateTime(TimeOnly.MinValue), // Store date part
            StartTime = startDateTime,
            EndTime = endDateTime,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            Status = RequestStatus.Pending
        };

        _db.Permissions.Add(permission);
        await _db.SaveChangesAsync(ct);

        // Send notifications to admin, HR, and superadmin
        var notificationMessage = $"طلب إذن بتاريخ {request.Date:yyyy-MM-dd} من {request.StartTime:HH:mm} إلى {request.EndTime:HH:mm}"
            + (string.IsNullOrWhiteSpace(permission.Reason) ? string.Empty : $". السبب: {permission.Reason}");
        await _notificationService.SendRequestNotificationAsync(
            userId,
            NotificationType.Permission,
            permission.Id,
            notificationMessage,
            ct);

        return permission;
    }

    public async Task<List<PermissionEntity>> GetUserPermissionsAsync(Guid userId, CancellationToken ct)
    {
        return await _db.Permissions
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }
      
    public async Task<List<PermissionEntity>> GetAllPermissionsAsync(CancellationToken ct)               
    {
        return await _db.Permissions
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<PaginatedResponse<PermissionResponse>> GetDepartmentPermissionsPaginatedAsync(
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
            throw new InvalidOperationException("Only Admin or SuperAdmin can access department permissions.");

        if (!currentUser.DepartmentId.HasValue)
        {
            return new PaginatedResponse<PermissionResponse>
            {
                Items = new List<PermissionResponse>(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = 0
            };
        }

        var query =
            from p in _db.Permissions
            join u in _db.Users on p.UserId equals u.Id
            where u.DepartmentId == currentUser.DepartmentId.Value && u.Role == AppRole.User
            select new { Permission = p, User = u };

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
                (x.Permission.Reason != null && x.Permission.Reason.ToLower().Contains(term)));
        }

        if (status.HasValue)
            query = query.Where(x => x.Permission.Status == status.Value);

        if (userId.HasValue)
            query = query.Where(x => x.Permission.UserId == userId.Value);

        if (dateFrom.HasValue)
        {
            var from = dateFrom.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.Permission.Date >= from);
        }

        if (dateTo.HasValue)
        {
            var to = dateTo.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(x => x.Permission.Date <= to);
        }

        var totalCount = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(x => x.Permission.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = rows.Select(x =>
        {
            var nameAr = string.Join(" ", new[] { x.User.FirstNameAr, x.User.MiddleNameAr, x.User.LastNameAr }
                .Where(n => !string.IsNullOrWhiteSpace(n)));
            var nameEn = string.Join(" ", new[] { x.User.FirstNameEn, x.User.MiddleNameEn, x.User.LastNameEn }
                .Where(n => !string.IsNullOrWhiteSpace(n)));

            return new PermissionResponse
            {
                Id = x.Permission.Id,
                UserId = x.Permission.UserId,
                EmployeeNameAr = string.IsNullOrWhiteSpace(nameAr) ? null : nameAr,
                EmployeeNameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn,
                Date = DateOnly.FromDateTime(x.Permission.Date),
                StartTime = TimeOnly.FromDateTime(x.Permission.StartTime),
                EndTime = TimeOnly.FromDateTime(x.Permission.EndTime),
                Reason = x.Permission.Reason,
                CreatedAt = x.Permission.CreatedAt,
                Status = x.Permission.Status.ToString(),
                RejectionReason = x.Permission.RejectionReason
            };
        }).ToList();

        return new PaginatedResponse<PermissionResponse>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PermissionEntity> UpdateStatusAsync(int permissionId, Guid currentUserId, UpdateStatusRequest request, CancellationToken ct)
    {
        var permission = await _db.Permissions.FindAsync(new object[] { permissionId }, ct);
        if (permission == null)
            throw new InvalidOperationException("Permission not found.");

        // Check authorization: Only SuperAdmin or Admin of the same department can update status
        var currentUser = await _db.Users.FindAsync(new object[] { currentUserId }, ct);
        if (currentUser == null)
            throw new InvalidOperationException("Current user not found.");

        var requestUser = await _db.Users.FindAsync(new object[] { permission.UserId }, ct);
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

        var oldStatus = permission.Status;
        permission.Status = request.Status;
        permission.RejectionReason = request.Status == RequestStatus.Rejected 
            ? request.RejectionReason?.Trim() 
            : null;

        await _db.SaveChangesAsync(ct);

        // Send notification to user if status changed
        if (oldStatus != request.Status && request.Status != RequestStatus.Pending)
        {
            await _notificationService.SendStatusChangeNotificationAsync(
                permission.UserId,
                NotificationType.Permission,
                permission.Id,
                request.Status,
                permission.RejectionReason,
                ct);
        }

        return permission;
    }

    public async Task SendReminderAsync(int permissionId, Guid currentUserId, CancellationToken ct)
    {
        var permission = await _db.Permissions.FindAsync(new object[] { permissionId }, ct);
        if (permission == null)
            throw new InvalidOperationException("Permission not found.");

        if (permission.UserId != currentUserId)
            throw new InvalidOperationException("You can only remind your own permission request.");

        if (permission.Status != RequestStatus.Pending)
            throw new InvalidOperationException("Permission request is not pending.");

        var requestDate = DateOnly.FromDateTime(permission.Date);
        var requestMessage = $"تذكير بطلب إذن بتاريخ {requestDate:yyyy-MM-dd} من {permission.StartTime:HH:mm} إلى {permission.EndTime:HH:mm}"
            + (string.IsNullOrWhiteSpace(permission.Reason) ? string.Empty : $". السبب: {permission.Reason}");

        await _notificationService.SendRequestReminderAsync(
            permission.UserId,
            NotificationType.Permission,
            permission.Id,
            requestMessage,
            ct);
    }
}
