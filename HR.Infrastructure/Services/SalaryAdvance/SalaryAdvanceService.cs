using internalEmployee.Auth.Contracts;
using internalEmployee.Auth.Models;
using internalEmployee.Data;
using internalEmployee.Data.Entities;
using SalaryAdvanceEntity = internalEmployee.Data.Entities.SalaryAdvance;
using internalEmployee.Services.Notification;
using Microsoft.EntityFrameworkCore;
using RequestStatus = internalEmployee.Data.Entities.RequestStatus;

namespace internalEmployee.Services.SalaryAdvance;

public sealed class SalaryAdvanceService : ISalaryAdvanceService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;

    public SalaryAdvanceService(AppDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<SalaryAdvanceEntity> CreateAdvanceRequestAsync(Guid userId, SalaryAdvanceRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        ValidateAdvanceRequest(user, request.Amount, request.MonthlyDeduction);

        var startDate = request.StartDate ?? DateOnly.FromDateTime(DateTime.Now);
        var numberOfMonths = CalculateNumberOfMonths(request.Amount, request.MonthlyDeduction);

        var advance = new SalaryAdvanceEntity
        {
            UserId = userId,
            Amount = request.Amount,
            MonthlyDeduction = request.MonthlyDeduction,
            NumberOfMonths = numberOfMonths,
            StartDate = startDate.ToDateTime(TimeOnly.MinValue),
            Reason = request.Reason?.Trim(),
            Status = RequestStatus.Pending,
            CreatedBy = userId
        };

        _db.SalaryAdvances.Add(advance);
        await _db.SaveChangesAsync(ct);

        var message = $"طلب سلفة بمبلغ {advance.Amount:N2} وقسط شهري {advance.MonthlyDeduction:N2} يبدأ من {startDate:yyyy-MM-dd}.";
        await _notificationService.SendRequestNotificationAsync(
            userId,
            NotificationType.SalaryAdvance,
            advance.Id,
            message,
            ct);

        return advance;
    }

    public async Task<SalaryAdvanceEntity> CreateManualAdvanceAsync(Guid currentUserId, SalaryAdvanceManualRequest request, CancellationToken ct)
    {
        var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == currentUserId, ct);
        if (currentUser == null)
            throw new InvalidOperationException("Current user not found.");

        if (currentUser.Role != AppRole.HR && currentUser.Role != AppRole.SuperAdmin)
            throw new InvalidOperationException("Only HR or SuperAdmin can create manual advances.");

        var employee = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.EmployeeId && u.IsActive, ct);
        if (employee == null)
            throw new InvalidOperationException("Employee not found.");

        ValidateAdvanceRequest(employee, request.Amount, request.MonthlyDeduction);

        var startDate = request.StartDate ?? DateOnly.FromDateTime(DateTime.Now);
        var numberOfMonths = CalculateNumberOfMonths(request.Amount, request.MonthlyDeduction);

        var advance = new SalaryAdvanceEntity
        {
            UserId = request.EmployeeId,
            Amount = request.Amount,
            MonthlyDeduction = request.MonthlyDeduction,
            NumberOfMonths = numberOfMonths,
            StartDate = startDate.ToDateTime(TimeOnly.MinValue),
            Reason = request.Reason?.Trim(),
            Status = RequestStatus.Approved,
            CreatedBy = currentUserId,
            ApprovedAt = DateTime.Now,
            ApprovedBy = currentUserId
        };

        _db.SalaryAdvances.Add(advance);
        await _db.SaveChangesAsync(ct);

        await _notificationService.SendStatusChangeNotificationAsync(
            advance.UserId,
            NotificationType.SalaryAdvance,
            advance.Id,
            RequestStatus.Approved,
            null,
            ct);

        return advance;
    }

    public async Task<List<SalaryAdvanceEntity>> GetUserAdvancesAsync(Guid userId, CancellationToken ct)
    {
        return await _db.SalaryAdvances
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<SalaryAdvanceEntity>> GetPendingAdvancesAsync(CancellationToken ct)
    {
        return await _db.SalaryAdvances
            .Where(a => a.Status == RequestStatus.Pending)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<SalaryAdvanceEntity>> GetAllAdvancesAsync(RequestStatus? status, CancellationToken ct)
    {
        var query = _db.SalaryAdvances.AsQueryable();
        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<SalaryAdvanceEntity> UpdateStatusAsync(int advanceId, Guid currentUserId, UpdateStatusRequest request, CancellationToken ct)
    {
        var advance = await _db.SalaryAdvances.FindAsync(new object[] { advanceId }, ct);
        if (advance == null)
            throw new InvalidOperationException("Salary advance not found.");

        var currentUser = await _db.Users.FindAsync(new object[] { currentUserId }, ct);
        if (currentUser == null)
            throw new InvalidOperationException("Current user not found.");

        var requestUser = await _db.Users.FindAsync(new object[] { advance.UserId }, ct);
        if (requestUser == null)
            throw new InvalidOperationException("Request user not found.");

        // Authorization: SuperAdmin, HR, or Admin of same department
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

        if (request.Status == RequestStatus.Approved)
        {
            ValidateAdvanceRequest(requestUser, advance.Amount, advance.MonthlyDeduction);
            advance.ApprovedAt = DateTime.Now;
            advance.ApprovedBy = currentUserId;
        }

        var oldStatus = advance.Status;
        advance.Status = request.Status;
        advance.RejectionReason = request.Status == RequestStatus.Rejected
            ? request.RejectionReason?.Trim()
            : null;

        await _db.SaveChangesAsync(ct);

        if (oldStatus != request.Status && request.Status != RequestStatus.Pending)
        {
            await _notificationService.SendStatusChangeNotificationAsync(
                advance.UserId,
                NotificationType.SalaryAdvance,
                advance.Id,
                request.Status,
                advance.RejectionReason,
                ct);
        }

        return advance;
    }

    private static int CalculateNumberOfMonths(decimal amount, decimal monthlyDeduction)
    {
        if (monthlyDeduction <= 0)
            return 0;
        return (int)Math.Ceiling(amount / monthlyDeduction);
    }

    private static void ValidateAdvanceRequest(AppUser user, decimal amount, decimal monthlyDeduction)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Amount must be greater than zero.");
        if (monthlyDeduction <= 0)
            throw new InvalidOperationException("Monthly deduction must be greater than zero.");
        if (monthlyDeduction > amount)
            throw new InvalidOperationException("Monthly deduction cannot exceed total amount.");

        var baseSalary = user.GrossSalary ?? user.ShiftRate;
        if (!baseSalary.HasValue)
            throw new InvalidOperationException("Employee salary is not configured.");
        if (monthlyDeduction > baseSalary.Value)
            throw new InvalidOperationException("Monthly deduction cannot exceed employee salary.");
    }
}
