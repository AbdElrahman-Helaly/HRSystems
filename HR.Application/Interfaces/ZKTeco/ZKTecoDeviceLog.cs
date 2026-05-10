namespace internalEmployee.Services.ZKTeco;

/// <summary>
/// Represents an attendance log entry from ZKTeco device
/// </summary>
public sealed class ZKTecoDeviceLog
{
    /// <summary>
    /// Machine code (Employee code) from the device
    /// </summary>
    public string MachineCode { get; set; } = string.Empty;

    /// <summary>
    /// Date of attendance
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Time of attendance/departure
    /// </summary>
    public TimeOnly Time { get; set; }

    /// <summary>
    /// Indicates if this is a check-in (true) or check-out (false)
    /// </summary>
    public bool IsCheckIn { get; set; }

    /// <summary>
    /// Location information (optional)
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Raw log data from device (optional, for debugging)
    /// </summary>
    public string? RawData { get; set; }
}

