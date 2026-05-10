namespace internalEmployee.Auth.Contracts;

/// <summary>
/// Response model for device log entry
/// </summary>
public sealed class DeviceLogResponse
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
    /// Time of attendance/departure
    /// </summary>
    public TimeOnly Time { get; init; }

    /// <summary>
    /// Indicates if this is a check-in (true) or check-out (false)
    /// </summary>
    public bool IsCheckIn { get; init; }

    /// <summary>
    /// Location information (optional)
    /// </summary>
    public string? Location { get; init; }
}

