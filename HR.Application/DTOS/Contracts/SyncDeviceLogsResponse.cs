namespace internalEmployee.Auth.Contracts;

/// <summary>
/// Response model for device logs synchronization
/// </summary>
public sealed class SyncDeviceLogsResponse
{
    /// <summary>
    /// Number of successfully synced logs
    /// </summary>
    public int SuccessCount { get; init; }

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; init; } = string.Empty;
}

