using internalEmployee.Auth.Contracts;

namespace internalEmployee.Services.UserLocation;

public interface IUserLocationService
{
    Task<UserLocationResponse> CreateAsync(CreateUserLocationRequest request, CancellationToken ct);
    Task<UserLocationResponse> UpdateAsync(int id, UpdateUserLocationRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<UserLocationResponse>> GetByUserAsync(Guid userId, CancellationToken ct);
    Task<UserLocationResponse?> GetByIdAsync(int id, CancellationToken ct);
}

