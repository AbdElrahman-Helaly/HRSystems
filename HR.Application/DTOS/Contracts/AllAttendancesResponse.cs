using internalEmployee.Data.Entities;

namespace internalEmployee.Auth.Contracts;

/// <summary>
/// Response model for all attendances with employee information
/// </summary>
public sealed class AllAttendancesResponse
{
    /// <summary>
    /// Attendance record ID
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Employee full name (Arabic or English)
    /// </summary>
    public string? EmployeeName { get; init; }

    /// <summary>
    /// Machine code (Employee code from device)
    /// </summary>
    public string? MachineCode { get; init; }

    /// <summary>
    /// Date of attendance
    /// </summary>
    public DateOnly Date { get; init; }

    /// <summary>
    /// Day of week name (Arabic and English)
    /// </summary>
    public string? DayOfWeek { get; init; }

    /// <summary>
    /// Time of check-in (attendance)
    /// </summary>
    public TimeOnly? AttendanceTime { get; init; }

    /// <summary>
    /// Time of check-out (departure)
    /// </summary>
    public TimeOnly? DepartureTime { get; init; }

    /// <summary>
    /// Device type (FingerprintDevice or Mobile)
    /// </summary>
    public AttendanceDeviceType? DeviceType { get; init; }

    /// <summary>
    /// Location information
    /// </summary>
    public string? Location { get; init; }

    /// <summary>
    /// Allowed location that matched this attendance (if any)
    /// </summary>
    public int? LocationId { get; init; }

    /// <summary>
    /// Record creation date
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Record last update date
    /// </summary>
    public DateTime? UpdatedAt { get; init; }
}

