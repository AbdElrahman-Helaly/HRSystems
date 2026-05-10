using internalEmployee.Auth.Contracts;
using internalEmployee.Data.Entities;
using AssignmentEntity = internalEmployee.Data.Entities.Assignment;

namespace internalEmployee.Services.Assignment;

public interface IAssignmentService
{
    Task<AssignmentEntity> CreateAssignmentAsync(Guid userId, AssignmentRequest request, CancellationToken ct);
    Task<List<AssignmentEntity>> GetUserAssignmentsAsync(Guid userId, CancellationToken ct);
    Task<List<AssignmentEntity>> GetAllAssignmentsAsync(CancellationToken ct);
    Task<PaginatedResponse<AssignmentResponse>> GetAllAssignmentsPaginatedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        RequestStatus? status,
        Guid? userId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken ct);
    Task<PaginatedResponse<AssignmentResponse>> GetDepartmentAssignmentsPaginatedAsync(
        Guid currentUserId,
        int pageNumber,
        int pageSize,
        string? search,
        RequestStatus? status,
        Guid? userId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken ct);
    Task<AssignmentEntity> UpdateStatusAsync(int assignmentId, Guid currentUserId, UpdateStatusRequest request, CancellationToken ct);
    Task SendReminderAsync(int assignmentId, Guid currentUserId, CancellationToken ct);
}
