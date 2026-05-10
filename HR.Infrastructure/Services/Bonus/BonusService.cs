using internalEmployee.Auth.Contracts;
using internalEmployee.Data;
using internalEmployee.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace internalEmployee.Services.Bonus;

public sealed class BonusService : IBonusService
{
    private readonly AppDbContext _db;

    public BonusService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<EmployeeBonus>> GetAllBonusesAsync(CancellationToken ct)
    {
        return await _db.EmployeeBonuses
            .OrderByDescending(b => b.BonusDate)
            .ThenByDescending(b => b.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<EmployeeBonus>> GetEmployeeBonusesAsync(Guid employeeId, CancellationToken ct)
    {
        return await _db.EmployeeBonuses
            .Where(b => b.UserId == employeeId)
            .OrderByDescending(b => b.BonusDate)
            .ThenByDescending(b => b.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<EmployeeBonus> GetBonusByIdAsync(int id, CancellationToken ct)
    {
        var bonus = await _db.EmployeeBonuses.FindAsync(new object[] { id }, ct);
        if (bonus == null)
            throw new InvalidOperationException("Bonus not found.");
        
        return bonus;
    }

    public async Task<EmployeeBonus> CreateBonusAsync(CreateBonusRequest request, Guid createdBy, CancellationToken ct)
    {
        // Validate employee exists
        var employeeExists = await _db.Users
            .AnyAsync(u => u.Id == request.UserId && u.IsActive, ct);
        
        if (!employeeExists)
            throw new InvalidOperationException("Employee not found or not active.");

        // Validate bonus amount
        if (request.Amount <= 0)
            throw new InvalidOperationException("Bonus amount must be greater than 0.");

        var bonus = new EmployeeBonus
        {
            UserId = request.UserId,
            Amount = request.Amount,
            BonusDate = request.BonusDate,
            Reason = request.Reason?.Trim(),
            CreatedBy = createdBy,
            CreatedAt = DateTime.Now
        };

        _db.EmployeeBonuses.Add(bonus);
        await _db.SaveChangesAsync(ct);

        return bonus;
    }

    public async Task<EmployeeBonus> UpdateBonusAsync(int id, UpdateBonusRequest request, CancellationToken ct)
    {
        var bonus = await _db.EmployeeBonuses.FindAsync(new object[] { id }, ct);
        if (bonus == null)
            throw new InvalidOperationException("Bonus not found.");

        if (request.Amount.HasValue)
        {
            if (request.Amount.Value <= 0)
                throw new InvalidOperationException("Bonus amount must be greater than 0.");
            bonus.Amount = request.Amount.Value;
        }

        if (request.BonusDate.HasValue)
        {
            bonus.BonusDate = request.BonusDate.Value;
        }

        if (request.Reason != null)
        {
            bonus.Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        }

        await _db.SaveChangesAsync(ct);

        return bonus;
    }

    public async Task DeleteBonusAsync(int id, CancellationToken ct)
    {
        var bonus = await _db.EmployeeBonuses.FindAsync(new object[] { id }, ct);
        if (bonus == null)
            throw new InvalidOperationException("Bonus not found.");

        _db.EmployeeBonuses.Remove(bonus);
        await _db.SaveChangesAsync(ct);
    }
}
