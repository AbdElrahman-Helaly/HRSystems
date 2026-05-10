using internalEmployee.Auth.Contracts;
using RequestStatus = internalEmployee.Data.Entities.RequestStatus;
using OvertimeEntity = internalEmployee.Data.Entities.Overtime;

namespace internalEmployee.Services.Overtime;

public interface IOvertimeService
{
    Task<OvertimeEntity> CreateOvertimeAsync(Guid userId, OvertimeRequest request, CancellationToken ct);
    Task<List<OvertimeEntity>> GetUserOvertimesAsync(Guid userId, CancellationToken ct);
    Task<List<OvertimeEntity>> GetPendingOvertimesAsync(CancellationToken ct);
    Task<List<OvertimeEntity>> GetAllOvertimesAsync(RequestStatus? status, CancellationToken ct);
    Task<OvertimeEntity> UpdateStatusAsync(int overtimeId, Guid currentUserId, UpdateStatusRequest request, CancellationToken ct);
}
