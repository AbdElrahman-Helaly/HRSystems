namespace internalEmployee.Auth.Contracts;

/// <summary>
/// Response model for all attendances with summary information
/// </summary>
public sealed class AllAttendancesWithSummaryResponse
{
    /// <summary>
    /// List of attendance records
    /// </summary>
    public List<AllAttendancesResponse> Attendances { get; init; } = new();

    /// <summary>
    /// Total number of active employees (after applying filters)
    /// </summary>
    public int TotalEmployees { get; init; }

    /// <summary>
    /// Number of employees who have attendance records (with AttendanceTime)
    /// </summary>
    public int EmployeesWithAttendance { get; init; }

    /// <summary>
    /// Number of employees who have departure records (with DepartureTime)
    /// </summary>
    public int EmployeesWithDeparture { get; init; }

    /// <summary>
    /// Total number of days in the period (from startDate to endDate)
    /// </summary>
    public int TotalDays { get; init; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int PageNumber { get; init; }

    /// <summary>
    /// Items per page
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of items (before pagination)
    /// </summary>
    public int TotalCount { get; init; }
}

