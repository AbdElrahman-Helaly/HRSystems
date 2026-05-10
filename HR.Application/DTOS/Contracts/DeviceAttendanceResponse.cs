namespace internalEmployee.Auth.Contracts;

/// <summary>
/// Response model for device attendance with both check-in and check-out times
/// </summary>
public sealed class DeviceAttendanceResponse
{
    /// <summary>
    /// Machine code (Employee code) from the device
    /// </summary>
    public string MachineCode { get; init; } = string.Empty;

    /// <summary>
    /// Date of attendance
    /// </summary>
    public DateOnly Date { get; init; }

    /// <summary>
    /// Time of check-in (nullable if no check-in recorded)
    /// </summary>
    public TimeOnly? CheckInTime { get; init; }

    /// <summary>
    /// Time of check-out (nullable if no check-out recorded)
    /// </summary>
    public TimeOnly? CheckOutTime { get; init; }

    /// <summary>
    /// Location information
    /// </summary>
    public string? Location { get; init; }
}

