namespace internalEmployee.Services.EmployeeHistory;

public interface IEmployeeHistoryService
{
    // Create history entry
    Task<Data.Entities.EmployeeHistory> CreateHistoryAsync(
        CreateEmployeeHistoryRequest request, 
        Guid? doneByUserId, 
        CancellationToken ct = default);
    
    // Get history for specific employee
    Task<EmployeeHistoryListResponse> GetEmployeeHistoryAsync(
        GetEmployeeHistoryRequest request, 
        CancellationToken ct = default);
    
    // Get history summary for employee
    Task<EmployeeHistorySummaryResponse> GetEmployeeHistorySummaryAsync(
        Guid employeeId, 
        CancellationToken ct = default);
    
    // Get specific history entry
    Task<EmployeeHistoryResponse?> GetHistoryByIdAsync(
        int historyId, 
        CancellationToken ct = default);
    
    // Helper methods to automatically track changes
    Task TrackJobChangeAsync(
        Guid employeeId, 
        int? oldJobId, 
        int? newJobId, 
        string? reason, 
        Guid? doneBy, 
        CancellationToken ct = default);
    
    Task TrackSalaryChangeAsync(
        Guid employeeId, 
        decimal? oldSalary, 
        decimal? newSalary, 
        string? reason, 
        Guid? doneBy, 
        CancellationToken ct = default);
    
    Task TrackDepartmentChangeAsync(
        Guid employeeId, 
        int? oldDepartmentId, 
        int? newDepartmentId, 
        string? reason, 
        Guid? doneBy, 
        CancellationToken ct = default);
    
    Task TrackBranchChangeAsync(
        Guid employeeId, 
        int? oldBranchId, 
        int? newBranchId, 
        string? reason, 
        Guid? doneBy, 
        CancellationToken ct = default);
    
    Task TrackManagerChangeAsync(
        Guid employeeId, 
        Guid? oldManagerId, 
        Guid? newManagerId, 
        string? reason, 
        Guid? doneBy, 
        CancellationToken ct = default);
    
    Task TrackPromotionAsync(
        Guid employeeId, 
        int? oldJobId, 
        int? newJobId, 
        decimal? oldSalary, 
        decimal? newSalary, 
        string? reason, 
        Guid? approvedBy, 
        CancellationToken ct = default);
}
