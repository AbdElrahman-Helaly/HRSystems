using internalEmployee.Auth.Contracts;

namespace internalEmployee.Services.HealthInsurance;

public interface IHealthInsuranceService
{
    Task<internalEmployee.Data.Entities.HealthInsuranceEnrollment> CreateAsync(Guid currentUserId, HealthInsuranceRequest request, CancellationToken ct);
    Task<List<internalEmployee.Data.Entities.HealthInsuranceEnrollment>> GetByUserAsync(Guid userId, CancellationToken ct);
    Task<List<internalEmployee.Data.Entities.HealthInsuranceEnrollment>> GetAllAsync(CancellationToken ct);
    Task<internalEmployee.Data.Entities.HealthInsuranceEnrollment> DeactivateAsync(int id, Guid currentUserId, CancellationToken ct);
}
