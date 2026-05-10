using internalEmployee.Auth.Contracts;
using internalEmployee.Data.Entities;

namespace internalEmployee.Services.InsuranceCompany;

public interface IInsuranceCompanyService
{
    Task<List<Data.Entities.InsuranceCompany>> GetAllAsync(CancellationToken ct);
    Task<Data.Entities.InsuranceCompany?> GetByIdAsync(int id, CancellationToken ct);
    Task<Data.Entities.InsuranceCompany> CreateAsync(CreateInsuranceCompanyRequest request, CancellationToken ct);
    Task<Data.Entities.InsuranceCompany> UpdateAsync(int id, UpdateInsuranceCompanyRequest request, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}
