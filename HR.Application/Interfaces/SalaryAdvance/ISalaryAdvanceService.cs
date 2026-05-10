using internalEmployee.Auth.Contracts;
using SalaryAdvanceEntity = internalEmployee.Data.Entities.SalaryAdvance;
using RequestStatus = internalEmployee.Data.Entities.RequestStatus;

namespace internalEmployee.Services.SalaryAdvance;

public interface ISalaryAdvanceService
{
    Task<SalaryAdvanceEntity> CreateAdvanceRequestAsync(Guid userId, SalaryAdvanceRequest request, CancellationToken ct);
    Task<SalaryAdvanceEntity> CreateManualAdvanceAsync(Guid currentUserId, SalaryAdvanceManualRequest request, CancellationToken ct);
    Task<List<SalaryAdvanceEntity>> GetUserAdvancesAsync(Guid userId, CancellationToken ct);
    Task<List<SalaryAdvanceEntity>> GetPendingAdvancesAsync(CancellationToken ct);
    Task<List<SalaryAdvanceEntity>> GetAllAdvancesAsync(RequestStatus? status, CancellationToken ct);
    Task<SalaryAdvanceEntity> UpdateStatusAsync(int advanceId, Guid currentUserId, UpdateStatusRequest request, CancellationToken ct);
}
