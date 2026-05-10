using internalEmployee.Auth.Contracts;
using System.Security.Claims;

namespace internalEmployee.Services.SuperAdminDashboard;

public interface ISuperAdminDashboardService
{
    Task<SuperAdminDashboardResponse> GetSuperAdminDashboardAsync(ClaimsPrincipal claimsPrincipal, CancellationToken ct);
}
