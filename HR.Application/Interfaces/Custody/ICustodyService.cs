using internalEmployee.Auth.Contracts;

namespace internalEmployee.Services.Custody;

public interface ICustodyService
{
    Task<CustodyItemResponse> CreateCustodyItemAsync(CreateCustodyItemRequest request, CancellationToken ct);
    Task<CustodyItemResponse> UpdateCustodyItemAsync(int id, UpdateCustodyItemRequest request, CancellationToken ct);
    Task<bool> DeleteCustodyItemAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<CustodyItemResponse>> GetCustodyItemsAsync(bool activeOnly, CancellationToken ct);
    Task<CustodyItemResponse?> GetCustodyItemByIdAsync(int id, CancellationToken ct);
    Task<EmployeeCustodyResponse> CreateEmployeeCustodyAsync(CreateEmployeeCustodyRequest request, CancellationToken ct);
    Task<EmployeeCustodyResponse> UpdateEmployeeCustodyAsync(int id, UpdateEmployeeCustodyRequest request, CancellationToken ct);
    Task<bool> DeleteEmployeeCustodyAsync(int id, CancellationToken ct);
    Task<PaginatedResponse<EmployeeCustodyResponse>> GetEmployeeCustodiesAsync(
        Guid? userId,
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken ct);
    Task<EmployeeCustodyResponse?> GetEmployeeCustodyByIdAsync(int id, CancellationToken ct);
    Task<EmployeeCustodyLookupsResponse> GetEmployeeCustodyLookupsAsync(CancellationToken ct);
}
