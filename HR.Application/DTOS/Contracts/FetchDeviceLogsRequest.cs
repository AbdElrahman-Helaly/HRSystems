namespace internalEmployee.Auth.Contracts;

/// <summary>
/// Request model for fetching attendance logs from ZKTeco device
/// IP and Port are fixed: 192.168.1.152:4370
/// </summary>
public sealed class FetchDeviceLogsRequest
{
    /// <summary>
    /// Start date for filtering logs
    /// </summary>
    public DateOnly StartDate { get; init; }

    /// <summary>
    /// End date for filtering logs
    /// </summary>
    public DateOnly EndDate { get; init; }

    /// <summary>
    /// Filter by check-in only (true), check-out only (false), or both (null)
    /// </summary>
    public bool? IsCheckInOnly { get; init; }
}

