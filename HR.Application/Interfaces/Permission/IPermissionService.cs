using internalEmployee.Auth.Contracts;
using internalEmployee.Data.Entities;
using PermissionEntity = internalEmployee.Data.Entities.Permission;

namespace internalEmployee.Services.Permission;

public interface IPermissionService
{
    Task<PermissionEntity> CreatePermissionAsync(Guid userId, PermissionRequest request, CancellationToken ct);
    Task<List<PermissionEntity>> GetUserPermissionsAsync(Guid userId, CancellationToken ct);
    Task<PermissionEntity> UpdateStatusAsync(int permissionId, Guid currentUserId, UpdateStatusRequest request, CancellationToken ct);
    Task<List<PermissionEntity>> GetAllPermissionsAsync(CancellationToken ct);
    Task<PaginatedResponse<PermissionResponse>> GetDepartmentPermissionsPaginatedAsync(
        Guid currentUserId,
        int pageNumber,
        int pageSize,
        string? search,
        RequestStatus? status,
        Guid? userId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken ct);
    Task SendReminderAsync(int permissionId, Guid currentUserId, CancellationToken ct);
}

