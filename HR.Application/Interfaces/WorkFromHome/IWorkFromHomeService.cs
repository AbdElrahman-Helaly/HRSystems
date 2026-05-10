using internalEmployee.Auth.Contracts;
using internalEmployee.Data.Entities;

namespace internalEmployee.Services.WorkFromHome;

public interface IWorkFromHomeService
{
    Task<WorkFromHomeRequest> CreateRequestAsync(Guid userId, WorkFromHomeCreateRequest request, CancellationToken ct);
    Task<List<WorkFromHomeRequest>> GetUserRequestsAsync(Guid userId, CancellationToken ct);
    Task<WorkFromHomeRequest> UpdateStatusAsync(int requestId, Guid currentUserId, UpdateStatusRequest request, CancellationToken ct);
    Task<PaginatedResponse<WorkFromHomeResponse>> GetDepartmentRequestsPaginatedAsync(
        Guid currentUserId,
        int pageNumber,
        int pageSize,
        string? search,
        RequestStatus? status,
        Guid? userId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken ct);
}
