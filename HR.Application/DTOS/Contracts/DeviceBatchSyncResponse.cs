namespace internalEmployee.Auth.Contracts;

/// <summary>
/// Batch response model for device sync operation
/// </summary>
public sealed class DeviceBatchSyncResponse
{
    /// <summary>
    /// Total number of records saved (new + updated)
    /// </summary>
    public int SavedCount { get; init; }

    /// <summary>
    /// Total number of records skipped (already exists with same data)
    /// </summary>
    public int SkippedCount { get; init; }

    /// <summary>
    /// Total number of records with MachineCode not found
    /// </summary>
    public int NotFoundCount { get; init; }

    /// <summary>
    /// List of MachineCodes that were not found
    /// </summary>
    public List<string> NotFoundMachineCodes { get; init; } = new();

    /// <summary>
    /// Detailed results for each record
    /// </summary>
    public List<DeviceSyncItemResponse> Results { get; init; } = new();
}
