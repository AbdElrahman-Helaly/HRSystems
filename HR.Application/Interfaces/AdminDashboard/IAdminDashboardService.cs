using internalEmployee.Auth.Contracts;
using System.Security.Claims;

namespace internalEmployee.Services.AdminDashboard;

public interface IAdminDashboardService
{
    Task<AdminDashboardResponse> GetAdminDashboardAsync(ClaimsPrincipal user, CancellationToken ct);
}

