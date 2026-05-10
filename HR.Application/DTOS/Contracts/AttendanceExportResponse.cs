namespace internalEmployee.Auth.Contracts;

/// <summary>
/// Response model for attendance export to Excel
/// </summary>
public sealed class AttendanceExportResponse
{
    /// <summary>
    /// Employee name
    /// </summary>
    public string EmployeeName { get; init; } = string.Empty;

    /// <summary>
    /// Gross salary
    /// </summary>
    public decimal GrossSalary { get; init; }

    /// <summary>
    /// Number of attendance days in the period (days with AttendanceTime or DepartureTime)
    /// </summary>
    public int AttendanceDays { get; init; }

    /// <summary>
    /// Total hours worked in the period
    /// </summary>
    public decimal TotalHoursWorked { get; init; }

    /// <summary>
    /// Number of absence days (excluding weekends for Full Time)
    /// </summary>
    public int AbsenceDays { get; init; }

    /// <summary>
    /// Employment mode name (Full Time / Part Time / Shift)
    /// </summary>
    public string? EmploymentModeName { get; init; }

    /// <summary>
    /// Total overtime hours worked
    /// </summary>
    public decimal TotalOvertimeHours { get; init; }
    
    /// <summary>
    /// Total late deduction hours (decimal)
    /// </summary>
    public decimal TotalLateDeductionHours { get; init; }

    /// <summary>
    /// Absence dates (comma-separated)
    /// </summary>
    public string? AbsenceDates { get; init; }
}

