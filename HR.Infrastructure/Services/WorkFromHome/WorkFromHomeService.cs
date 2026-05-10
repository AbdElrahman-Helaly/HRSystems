using internalEmployee.Auth.Contracts;
using internalEmployee.Auth.Models;
using internalEmployee.Data;
using internalEmployee.Data.Entities;
using internalEmployee.Services.Notification;
using Microsoft.EntityFrameworkCore;

namespace internalEmployee.Services.WorkFromHome;

public sealed class WorkFromHomeService : IWorkFromHomeService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;

    public WorkFromHomeService(AppDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<WorkFromHomeRequest> CreateRequestAsync(Guid userId, WorkFromHomeCreateRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        if (request.StartDate > request.EndDate)
            throw new InvalidOperationException("تاريخ البداية يجب أن يكون قبل تاريخ النهاية.");

        var wfhRequest = new WorkFromHomeRequest
        {
            UserId = userId,
            StartDate = request.StartDate.Date,
            EndDate = request.EndDate.Date,
            Reason = request.Reason?.Trim(),
            Status = RequestStatus.Pending
        };

        _db.WorkFromHomeRequests.Add(wfhRequest);
        await _db.SaveChangesAsync(ct);

        // Notify manager/Admins/HR
        var message = $"طلب عمل من المنزل من {wfhRequest.StartDate:yyyy-MM-dd} إلى {wfhRequest.EndDate:yyyy-MM-dd}";
        if (!string.IsNullOrWhiteSpace(wfhRequest.Reason))
            message += $". السبب: {wfhRequest.Reason}";

        await _notificationService.SendRequestNotificationAsync(
            userId,
            NotificationType.WorkFromHome,
            wfhRequest.Id,
            message,
            ct);

        return wfhRequest;
    }

    public async Task<List<WorkFromHomeRequest>> GetUserRequestsAsync(Guid userId, CancellationToken ct)
    {
        return await _db.WorkFromHomeRequests
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<WorkFromHomeRequest> UpdateStatusAsync(int requestId, Guid currentUserId, UpdateStatusRequest request, CancellationToken ct)
    {
        var wfhRequest = await _db.WorkFromHomeRequests.FindAsync(new object[] { requestId }, ct);
        if (wfhRequest == null)
            throw new InvalidOperationException("Request not found.");

        var currentUser = await _db.Users.FindAsync(new object[] { currentUserId }, ct);
        if (currentUser == null)
            throw new InvalidOperationException("Current user not found.");

        var requestUser = await _db.Users.FindAsync(new object[] { wfhRequest.UserId }, ct);
        if (requestUser == null)
            throw new InvalidOperationException("Request user not found.");

        // Authorization check
        if (currentUser.Role != AppRole.SuperAdmin && currentUser.Role != AppRole.HR)
        {
            if (currentUser.Role == AppRole.Admin)
            {
                if (currentUser.DepartmentId == null || requestUser.DepartmentId == null || 
                    currentUser.DepartmentId != requestUser.DepartmentId)
                    throw new InvalidOperationException("You can only update requests from users in your department.");
            }
            else
            {
                throw new InvalidOperationException("Only SuperAdmin, HR, or Department Manager can update request status.");
            }
        }

        var oldStatus = wfhRequest.Status;
        wfhRequest.Status = request.Status;
        wfhRequest.RejectionReason = request.Status == RequestStatus.Rejected 
            ? request.RejectionReason?.Trim() 
            : null;

        await _db.SaveChangesAsync(ct);

        if (oldStatus != request.Status && request.Status != RequestStatus.Pending)
        {
            await _notificationService.SendStatusChangeNotificationAsync(
                wfhRequest.UserId,
                NotificationType.WorkFromHome,
                wfhRequest.Id,
                request.Status,
                wfhRequest.RejectionReason,
                ct);
        }

        return wfhRequest;
    }

    public async Task<PaginatedResponse<WorkFromHomeResponse>> GetDepartmentRequestsPaginatedAsync(
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
        var currentUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId, ct);
        if (currentUser == null) throw new InvalidOperationException("User not found.");

        var query = from r in _db.WorkFromHomeRequests
                    join u in _db.Users on r.UserId equals u.Id
                    select new { Request = r, User = u };

        if (currentUser.Role == AppRole.Admin)
        {
            query = query.Where(x => x.User.DepartmentId == currentUser.DepartmentId);
        }
        else if (currentUser.Role != AppRole.SuperAdmin && currentUser.Role != AppRole.HR)
        {
            throw new InvalidOperationException("Unauthorized access.");
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => 
                (x.User.FirstNameAr != null && x.User.FirstNameAr.ToLower().Contains(term)) ||
                (x.User.LastNameAr != null && x.User.LastNameAr.ToLower().Contains(term)) ||
                (x.User.FirstNameEn != null && x.User.FirstNameEn.ToLower().Contains(term)) ||
                (x.User.LastNameEn != null && x.User.LastNameEn.ToLower().Contains(term)));
        }

        if (status.HasValue) query = query.Where(x => x.Request.Status == status.Value);
        if (userId.HasValue) query = query.Where(x => x.Request.UserId == userId.Value);
        
        if (dateFrom.HasValue)
        {
            var from = dateFrom.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.Request.StartDate >= from);
        }

        if (dateTo.HasValue)
        {
            var to = dateTo.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(x => x.Request.EndDate <= to);
        }

        var totalCount = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(x => x.Request.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = rows.Select(x => new WorkFromHomeResponse
        {
            Id = x.Request.Id,
            UserId = x.Request.UserId,
            EmployeeName = $"{x.User.FirstNameAr} {x.User.LastNameAr}".Trim(),
            StartDate = x.Request.StartDate,
            EndDate = x.Request.EndDate,
            Reason = x.Request.Reason,
            Status = x.Request.Status,
            CreatedAt = x.Request.CreatedAt,
            RejectionReason = x.Request.RejectionReason
        }).ToList();

        return new PaginatedResponse<WorkFromHomeResponse>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
