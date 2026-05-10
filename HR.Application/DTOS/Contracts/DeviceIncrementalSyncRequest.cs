namespace internalEmployee.Auth.Contracts;

public sealed class DeviceIncrementalSyncRequest
{
    /// <summary>
    /// Start date (required for first-time sync).
    /// </summary>
    public DateOnly? StartDate { get; init; }

    /// <summary>
    /// End date (optional). Defaults to today.
    /// </summary>
    public DateOnly? EndDate { get; init; }

    /// <summary>
    /// Filter by check-in only (true), check-out only (false), or both (null)
    /// </summary>
    public bool? IsCheckInOnly { get; init; }
}
