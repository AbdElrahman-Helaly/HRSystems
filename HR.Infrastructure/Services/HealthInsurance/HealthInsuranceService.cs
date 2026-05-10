using internalEmployee.Auth.Contracts;
using internalEmployee.Auth.Models;
using internalEmployee.Data;
using internalEmployee.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace internalEmployee.Services.HealthInsurance;

public sealed class HealthInsuranceService : IHealthInsuranceService
{
    private readonly AppDbContext _db;

    public HealthInsuranceService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<HealthInsuranceEnrollment> CreateAsync(Guid currentUserId, HealthInsuranceRequest request, CancellationToken ct)
    {
        var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == currentUserId, ct);
        if (currentUser == null)
            throw new InvalidOperationException("Current user not found.");

        if (currentUser.Role != AppRole.HR && currentUser.Role != AppRole.SuperAdmin)
            throw new InvalidOperationException("Only HR or SuperAdmin can add health insurance.");

        var employee = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.EmployeeId && u.IsActive, ct);
        if (employee == null)
            throw new InvalidOperationException("Employee not found.");

        if (request.EndDate.HasValue && request.StartDate.HasValue && request.EndDate.Value < request.StartDate.Value)
            throw new InvalidOperationException("EndDate cannot be before StartDate.");

        var startDate = request.StartDate ?? DateOnly.FromDateTime(DateTime.Now);

        var enrollment = new HealthInsuranceEnrollment
        {
            UserId = request.EmployeeId,
            MonthlyPremium = request.MonthlyPremium,
            StartDate = startDate.ToDateTime(TimeOnly.MinValue),
            EndDate = request.EndDate?.ToDateTime(TimeOnly.MinValue),
            Notes = request.Notes?.Trim(),
            IsActive = true,
            CreatedBy = currentUserId
        };

        _db.HealthInsuranceEnrollments.Add(enrollment);
        await _db.SaveChangesAsync(ct);

        return enrollment;
    }

    public async Task<List<HealthInsuranceEnrollment>> GetByUserAsync(Guid userId, CancellationToken ct)
    {
        return await _db.HealthInsuranceEnrollments
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<HealthInsuranceEnrollment>> GetAllAsync(CancellationToken ct)
    {
        return await _db.HealthInsuranceEnrollments
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<HealthInsuranceEnrollment> DeactivateAsync(int id, Guid currentUserId, CancellationToken ct)
    {
        var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == currentUserId, ct);
        if (currentUser == null)
            throw new InvalidOperationException("Current user not found.");

        if (currentUser.Role != AppRole.HR && currentUser.Role != AppRole.SuperAdmin)
            throw new InvalidOperationException("Only HR or SuperAdmin can deactivate health insurance.");

        var enrollment = await _db.HealthInsuranceEnrollments.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (enrollment == null)
            throw new InvalidOperationException("Health insurance record not found.");

        enrollment.IsActive = false;
        enrollment.EndDate = DateOnly.FromDateTime(DateTime.Now).ToDateTime(TimeOnly.MinValue);

        await _db.SaveChangesAsync(ct);
        return enrollment;
    }
}
