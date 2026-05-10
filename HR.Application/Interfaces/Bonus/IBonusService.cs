using internalEmployee.Auth.Contracts;
using internalEmployee.Data.Entities;

namespace internalEmployee.Services.Bonus;

public interface IBonusService
{
    Task<List<EmployeeBonus>> GetAllBonusesAsync(CancellationToken ct);
    Task<List<EmployeeBonus>> GetEmployeeBonusesAsync(Guid employeeId, CancellationToken ct);
    Task<EmployeeBonus> GetBonusByIdAsync(int id, CancellationToken ct);
    Task<EmployeeBonus> CreateBonusAsync(CreateBonusRequest request, Guid createdBy, CancellationToken ct);
    Task<EmployeeBonus> UpdateBonusAsync(int id, UpdateBonusRequest request, CancellationToken ct);
    Task DeleteBonusAsync(int id, CancellationToken ct);
}
