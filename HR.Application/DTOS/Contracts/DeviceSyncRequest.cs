using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

/// <summary>
/// Request model for syncing device attendance data from console app
/// </summary>
public sealed class DeviceSyncRequest
{
    /// <summary>
    /// Machine code (Employee code) from ZKTeco device
    /// </summary>
    [Required]
    public string MachineCode { get; init; } = string.Empty;

    /// <summary>
    /// Date of attendance
    /// </summary>
    [Required]
    public DateOnly Date { get; init; }

    /// <summary>
    /// Time of check-in (optional)
    /// </summary>
    public TimeOnly? CheckInTime { get; init; }

    /// <summary>
    /// Time of check-out (optional)
    /// </summary>
    public TimeOnly? CheckOutTime { get; init; }
}
