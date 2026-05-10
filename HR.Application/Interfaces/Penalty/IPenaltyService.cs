using internalEmployee.Auth.Contracts;
using internalEmployee.Data.Entities;

namespace internalEmployee.Services.Penalty;

public interface IPenaltyService
{
    Task<List<PenaltyType>> GetPenaltyTypesAsync(CancellationToken ct);
    Task<List<EmployeePenalty>> GetEmployeePenaltiesAsync(Guid employeeId, CancellationToken ct);
    Task<List<EmployeePenalty>> GetPendingPenaltiesAsync(Guid employeeId, CancellationToken ct);
    Task<EmployeePenalty> CreatePenaltyAsync(CreatePenaltyRequest request, Guid createdBy, CancellationToken ct);
    Task<EmployeePenalty> UpdatePenaltyAsync(int id, UpdatePenaltyRequest request, CancellationToken ct);
    Task DeletePenaltyAsync(int id, CancellationToken ct);
}
