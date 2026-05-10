using internalEmployee.Auth.Contracts;
using internalEmployee.Data;
using internalEmployee.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace internalEmployee.Services.Penalty;

public sealed class PenaltyService : IPenaltyService
{
    private readonly AppDbContext _db;

    public PenaltyService(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<PenaltyType>> GetPenaltyTypesAsync(CancellationToken ct)
    {
        var types = Enum.GetValues(typeof(PenaltyType))
            .Cast<PenaltyType>()
            .ToList();
        return Task.FromResult(types);
    }

    public async Task<List<EmployeePenalty>> GetEmployeePenaltiesAsync(Guid employeeId, CancellationToken ct)
    {
        return await _db.EmployeePenalties
            .Where(p => p.UserId == employeeId)
            .OrderByDescending(p => p.PenaltyDate)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<EmployeePenalty>> GetPendingPenaltiesAsync(Guid employeeId, CancellationToken ct)
    {
        return await _db.EmployeePenalties
            .Where(p => p.UserId == employeeId && !p.IsApplied)
            .OrderBy(p => p.PenaltyDate)
            .ToListAsync(ct);
    }

    public async Task<EmployeePenalty> CreatePenaltyAsync(CreatePenaltyRequest request, Guid createdBy, CancellationToken ct)
    {
        // Validate employee exists
        var employeeExists = await _db.Users
            .AnyAsync(u => u.Id == request.UserId && u.IsActive, ct);
        
        if (!employeeExists)
            throw new InvalidOperationException("Employee not found or not active.");

        // Validate penalty data
        if (request.PenaltyType == PenaltyType.Days)
        {
            if (!request.Days.HasValue || request.Days.Value <= 0)
                throw new InvalidOperationException("Days must be greater than 0 when PenaltyType is Days.");
        }
        else if (request.PenaltyType == PenaltyType.FixedAmount)
        {
            if (!request.Amount.HasValue || request.Amount.Value <= 0)
                throw new InvalidOperationException("Amount must be greater than 0 when PenaltyType is FixedAmount.");
        }

        var penalty = new EmployeePenalty
        {
            UserId = request.UserId,
            PenaltyType = request.PenaltyType,
            Days = request.Days,
            Amount = request.Amount,
            PenaltyDate = request.PenaltyDate,
            Reason = request.Reason?.Trim(),
            CreatedBy = createdBy,
            IsApplied = false,
            CreatedAt = DateTime.Now
        };

        _db.EmployeePenalties.Add(penalty);
        await _db.SaveChangesAsync(ct);

        return penalty;
    }

    public async Task<EmployeePenalty> UpdatePenaltyAsync(int id, UpdatePenaltyRequest request, CancellationToken ct)
    {
        var penalty = await _db.EmployeePenalties.FindAsync(new object[] { id }, ct);
        if (penalty == null)
            throw new InvalidOperationException("Penalty not found.");

        // Don't allow updating applied penalties
        if (penalty.IsApplied)
            throw new InvalidOperationException("Cannot update an applied penalty.");

        if (request.PenaltyType.HasValue)
        {
            penalty.PenaltyType = request.PenaltyType.Value;
        }

        if (request.Days.HasValue)
        {
            if (penalty.PenaltyType == PenaltyType.Days && request.Days.Value <= 0)
                throw new InvalidOperationException("Days must be greater than 0.");
            penalty.Days = request.Days;
        }

        if (request.Amount.HasValue)
        {
            if (penalty.PenaltyType == PenaltyType.FixedAmount && request.Amount.Value <= 0)
                throw new InvalidOperationException("Amount must be greater than 0.");
            penalty.Amount = request.Amount;
        }

        if (request.PenaltyDate.HasValue)
        {
            penalty.PenaltyDate = request.PenaltyDate.Value;
        }

        if (request.Reason != null)
        {
            penalty.Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        }

        await _db.SaveChangesAsync(ct);

        return penalty;
    }

    public async Task DeletePenaltyAsync(int id, CancellationToken ct)
    {
        var penalty = await _db.EmployeePenalties.FindAsync(new object[] { id }, ct);
        if (penalty == null)
            throw new InvalidOperationException("Penalty not found.");

        // Don't allow deleting applied penalties
        if (penalty.IsApplied)
            throw new InvalidOperationException("Cannot delete an applied penalty.");

        _db.EmployeePenalties.Remove(penalty);
        await _db.SaveChangesAsync(ct);
    }
}
