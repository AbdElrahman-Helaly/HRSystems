using internalEmployee.Auth.Contracts;
using internalEmployee.Auth.Models;
using internalEmployee.Data;
using internalEmployee.Data.Entities;
using internalEmployee.Services.Notification;
using Microsoft.EntityFrameworkCore;
using OvertimeEntity = internalEmployee.Data.Entities.Overtime;
using RequestStatus = internalEmployee.Data.Entities.RequestStatus;

namespace internalEmployee.Services.Overtime;

public sealed class OvertimeService : IOvertimeService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;

    public OvertimeService(AppDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<OvertimeEntity> CreateOvertimeAsync(Guid userId, OvertimeRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        var startDateTime = request.Date.ToDateTime(request.StartTime);
        var endDateTime = request.Date.ToDateTime(request.EndTime);

        if (endDateTime <= startDateTime)
            throw new InvalidOperationException("وقت النهاية يجب أن يكون بعد وقت البداية.");

        var totalHours = (decimal)(endDateTime - startDateTime).TotalHours;
        var overtime = new OvertimeEntity
        {
            UserId = userId,
            Date = request.Date.ToDateTime(TimeOnly.MinValue),
            StartTime = startDateTime,
            EndTime = endDateTime,
            TotalHours = totalHours,
            HourlyRate = 0m,
            Amount = 0m,
            Reason = request.Reason.Trim(),
            Status = RequestStatus.Pending
        };

        _db.Overtimes.Add(overtime);
        await _db.SaveChangesAsync(ct);

        var notificationMessage =
            $"طلب عمل إضافي بتاريخ {request.Date:yyyy-MM-dd} من {request.StartTime:HH:mm} إلى {request.EndTime:HH:mm}. السبب: {overtime.Reason}";

        await _notificationService.SendRequestNotificationAsync(
            userId,
            NotificationType.Overtime,
            overtime.Id,
            notificationMessage,
            ct);

        return overtime;
    }

    public async Task<List<OvertimeEntity>> GetUserOvertimesAsync(Guid userId, CancellationToken ct)
    {
        return await _db.Overtimes
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<OvertimeEntity>> GetPendingOvertimesAsync(CancellationToken ct)
    {
        return await _db.Overtimes
            .Where(o => o.Status == RequestStatus.Pending)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<OvertimeEntity>> GetAllOvertimesAsync(RequestStatus? status, CancellationToken ct)
    {
        var query = _db.Overtimes.AsQueryable();
        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<OvertimeEntity> UpdateStatusAsync(int overtimeId, Guid currentUserId, UpdateStatusRequest request, CancellationToken ct)
    {
        var overtime = await _db.Overtimes.FindAsync(new object[] { overtimeId }, ct);
        if (overtime == null)
            throw new InvalidOperationException("Overtime request not found.");

        var currentUser = await _db.Users.FindAsync(new object[] { currentUserId }, ct);
        if (currentUser == null)
            throw new InvalidOperationException("Current user not found.");

        var requestUser = await _db.Users.FindAsync(new object[] { overtime.UserId }, ct);
        if (requestUser == null)
            throw new InvalidOperationException("Request user not found.");

        if (currentUser.Role != AppRole.SuperAdmin)
        {
            if (currentUser.Role == AppRole.Admin)
            {
                if (currentUser.DepartmentId == null || requestUser.DepartmentId == null ||
                    currentUser.DepartmentId != requestUser.DepartmentId)
                    throw new InvalidOperationException("You can only update requests from users in your department.");
            }
            else
            {
                throw new InvalidOperationException("Only SuperAdmin or Department Manager can update request status.");
            }
        }

        var oldStatus = overtime.Status;
        overtime.Status = request.Status;
        overtime.RejectionReason = request.Status == RequestStatus.Rejected
            ? request.RejectionReason?.Trim()
            : null;

        if (request.Status == RequestStatus.Approved)
        {
            if (!requestUser.GrossSalary.HasValue)
                throw new InvalidOperationException("Employee does not have a salary configured.");

            var totalHours = (decimal)(overtime.EndTime - overtime.StartTime).TotalHours;
            if (totalHours <= 0)
                throw new InvalidOperationException("Invalid overtime duration.");

            var hourlyRate = requestUser.GrossSalary.Value / (8m * 30m);
            var overtimeMultiplier = requestUser.OvertimeRate ?? 1m;

            overtime.TotalHours = totalHours;
            overtime.HourlyRate = hourlyRate;
            overtime.Amount = totalHours * hourlyRate * overtimeMultiplier;
        }

        await _db.SaveChangesAsync(ct);

        if (oldStatus != request.Status && request.Status != RequestStatus.Pending)
        {
            await _notificationService.SendStatusChangeNotificationAsync(
                overtime.UserId,
                NotificationType.Overtime,
                overtime.Id,
                request.Status,
                overtime.RejectionReason,
                ct);
        }

        return overtime;
    }
}
