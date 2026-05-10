using internalEmployee.Auth.Contracts;
using internalEmployee.Data.Entities;
using LeaveEntity = internalEmployee.Data.Entities.Leave;
using Microsoft.AspNetCore.Http;

namespace internalEmployee.Services.Leave;

public interface ILeaveService
{
    Task<LeaveEntity> CreateLeaveAsync(Guid userId, LeaveRequest request, IFormFile? medicalReport, Guid? doneByUserId, CancellationToken ct);
    Task<List<LeaveEntity>> GetUserLeavesAsync(Guid userId, CancellationToken ct);
    Task<List<LeaveEntity>> GetAllPendingLeavesAsync(CancellationToken ct);
    Task<LeaveEntity> UpdateStatusAsync(int leaveId, Guid currentUserId, UpdateStatusRequest request, CancellationToken ct);
    Task SendReminderAsync(int leaveId, Guid currentUserId, CancellationToken ct);
    Task<LeaveBalanceResponse> GetLeaveBalanceAsync(Guid userId, CancellationToken ct);
    Task<PaginatedEmployeeLeaveBalanceResponse> GetAllEmployeesWithLeaveBalanceAsync(string? search, Guid? userId, int pageNumber, int pageSize, CancellationToken ct);
    Task<PaginatedResponse<LeaveResponse>> GetDepartmentLeavesPaginatedAsync(
        Guid currentUserId,
        int pageNumber,
        int pageSize,
        string? search,
        RequestStatus? status,
        Guid? userId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken ct);
    Task UpdateSickLeaveBalanceAsync(Guid employeeId, decimal balance, DateOnly startDate, DateOnly endDate, CancellationToken ct);
}

