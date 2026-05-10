using internalEmployee.Auth.Contracts;
using AttendanceEntity = internalEmployee.Data.Entities.Attendance;

namespace internalEmployee.Services.Attendance;

public interface IAttendanceService
{
    Task<AttendanceEntity> CreateAttendanceAsync(Guid userId, CreateAttendanceRequest request, CancellationToken ct);
    Task<AttendanceEntity> CreateDepartureAsync(Guid userId, CreateDepartureRequest request, CancellationToken ct);
    
    // Device (ZKTeco) attendance methods
    Task<AttendanceEntity> CreateDeviceAttendanceAsync(DeviceAttendanceRequest request, CancellationToken ct);
    Task<AttendanceEntity> CreateDeviceDepartureAsync(DeviceDepartureRequest request, CancellationToken ct);
    
    /// <summary>
    /// Syncs device attendance data from console app with duplicate check
    /// </summary>
    /// <param name="request">Device sync request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Sync response indicating if record was created, updated, or skipped</returns>
    Task<DeviceSyncResponse> SyncDeviceAttendanceAsync(DeviceSyncRequest request, CancellationToken ct);
    
    /// <summary>
    /// Syncs multiple device attendance records in batch
    /// </summary>
    /// <param name="request">Batch sync request containing list of records</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Batch sync response with counts and list of not found MachineCodes</returns>
    Task<DeviceBatchSyncResponse> SyncDeviceAttendanceBatchAsync(DeviceBatchSyncRequest request, CancellationToken ct);
    
    // Mobile attendance methods
    Task<AttendanceEntity> CreateMobileAttendanceAsync(Guid userId, MobileAttendanceRequest request, CancellationToken ct);
    Task<AttendanceEntity> CreateMobileDepartureAsync(Guid userId, MobileDepartureRequest request, CancellationToken ct);
    
    /// <summary>
    /// Records manual attendance (check-in and check-out) for an employee
    /// </summary>
    /// <param name="request">Manual attendance request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created or updated attendance record</returns>
    Task<AttendanceEntity> RecordManualAttendanceAsync(ManualAttendanceRequest request, CancellationToken ct);

    Task<List<AttendanceEntity>> GetUserAttendancesAsync(Guid userId, DateOnly? startDate, DateOnly? endDate, CancellationToken ct);
    Task<AttendanceEntity?> GetAttendanceByDateAsync(Guid userId, DateOnly date, CancellationToken ct);
    /// <summary>
    /// Deletes an attendance record by its Id.
    /// </summary>
    /// <param name="attendanceId">Attendance record Id</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Task</returns>
   // Task DeleteAttendanceAsync(Guid attendanceId, CancellationToken ct);
    Task<List<AttendanceEntity>> GetAllAttendancesAsync(Guid userId, int? month, CancellationToken ct);
    Task<List<AttendanceEntity>> GetAllAttendancesWithDateFilterAsync(Guid userId, DateOnly? startDate, DateOnly? endDate, CancellationToken ct);
    Task<List<AttendanceEntity>> GetUserAttendanceRecordsAsync(Guid userId, int? month, CancellationToken ct);
    Task<List<AttendanceEntity>> GetUserDepartureRecordsAsync(Guid userId, int? month, CancellationToken ct);
    Task<MonthlyAttendanceReportResponse> GetMonthlyReportAsync(Guid userId, int month, int year, CancellationToken ct);
    
    /// <summary>
    /// Saves device attendance logs to database
    /// </summary>
    /// <param name="logs">List of device attendance logs</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of successfully saved records</returns>
    Task<int> SaveDeviceAttendanceLogsAsync(List<DeviceAttendanceResponse> logs, CancellationToken ct);
    
    /// <summary>
    /// Gets all attendances with employee information
    /// </summary>
    /// <param name="startDate">Start date filter (optional)</param>
    /// <param name="endDate">End date filter (optional)</param>
    /// <param name="machineCode">Machine code filter (optional)</param>
    /// <param name="employeeId">Employee ID filter (optional)</param>
    /// <param name="isCheckIn">Filter by check-in/check-out (true = attendance only, false = departure only, null = all)</param>
    /// <param name="pageNumber">Page number (optional)</param>
    /// <param name="pageSize">Items per page (optional)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of attendances with employee information</returns>
    Task<List<AllAttendancesResponse>> GetAllAttendancesWithUserInfoAsync(DateOnly? startDate, DateOnly? endDate, string? machineCode, Guid? employeeId, bool? isCheckIn, int pageNumber, int pageSize, CancellationToken ct);
    
    /// <summary>
    /// Gets all attendances with employee information and summary statistics
    /// </summary>
    /// <param name="startDate">Start date filter (optional)</param>
    /// <param name="endDate">End date filter (optional)</param>
    /// <param name="machineCode">Machine code filter (optional)</param>
    /// <param name="employeeId">Employee ID filter (optional)</param>
    /// <param name="isCheckIn">Filter by check-in/check-out (true = attendance only, false = departure only, null = all)</param>
    /// <param name="pageNumber">Page number (optional)</param>
    /// <param name="pageSize">Items per page (optional)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Attendances with summary information</returns>
    Task<AllAttendancesWithSummaryResponse> GetAllAttendancesWithSummaryAsync(DateOnly? startDate, DateOnly? endDate, string? machineCode, Guid? employeeId, bool? isCheckIn, int pageNumber, int pageSize, CancellationToken ct);
    
    /// <summary>
    /// Gets attendance export data for Excel
    /// </summary>
    /// <param name="month">Month (1-12)</param>
    /// <param name="year">Year</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of attendance export data</returns>
    Task<List<AttendanceExportResponse>> GetAttendanceExportDataAsync(int month, int year, CancellationToken ct);
    
    /// <summary>
    /// Calculates employee salary based on attendance records
    /// </summary>
    /// <param name="request">Salary calculation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Calculated salary response</returns>
    Task<CalculateSalaryResponse> CalculateSalaryAsync(CalculateSalaryRequest request, CancellationToken ct);
    
    /// <summary>
    /// Gets monthly report for all employees
    /// </summary>
    /// <param name="month">Month (1-12)</param>
    /// <param name="year">Year</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of monthly report responses</returns>
    Task<List<MonthlyReportResponse>> GetMonthlyReportForAllEmployeesAsync(int month, int year, CancellationToken ct);

    /// <summary>
    /// Gets detailed monthly report for all employees (with absence/late dates)
    /// </summary>
    Task<List<MonthlyReportDetailedResponse>> GetMonthlyReportDetailedForAllEmployeesAsync(int month, int year, CancellationToken ct);
    
    /// <summary>
    /// Gets monthly shift report for all employees who are in Shift mode
    /// </summary>
    Task<List<ShiftMonthlyReportResponse>> GetShiftMonthlyReportAsync(int month, int year, CancellationToken ct);

    /// <summary>
    /// Gets monthly shift report items (one row per shift day)
    /// </summary>
    Task<List<ShiftMonthlyReportItemResponse>> GetShiftMonthlyReportItemsAsync(int month, int year, CancellationToken ct);
    /// <summary>
    /// Gets a detailed payslip for an employee
    /// </summary>
    /// <param name="userId">Employee ID</param>
    /// <param name="month">Month (1-12)</param>
    /// <param name="year">Year</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Detailed payslip response</returns>
    Task<PayslipResponse> GetPayslipAsync(Guid userId, int month, int year, CancellationToken ct);
}
