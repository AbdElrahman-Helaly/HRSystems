using internalEmployee.Auth.Contracts;
using System.Security.Claims;

namespace internalEmployee.Services.Home;

public interface IHomeService
{
    Task<HomeResponse> GetHomeAsync(ClaimsPrincipal user, CancellationToken ct);
    Task<HRHomeResponse> GetHRHomeAsync(ClaimsPrincipal user, CancellationToken ct);
    Task<List<RequestItem>> GetAllRequestsAsync(ClaimsPrincipal user, int? month, CancellationToken ct);
    Task<List<RequestItem>> GetAllPendingRequestsAsync(ClaimsPrincipal user, int? month, CancellationToken ct);
    Task<List<RequestItem>> GetAllAcceptedRequestsAsync(ClaimsPrincipal user, int? month, CancellationToken ct);
    Task<List<RequestItem>> GetAllRejectedRequestsAsync(ClaimsPrincipal user, int? month, CancellationToken ct);
}

