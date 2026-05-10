using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

/// <summary>
/// Batch request model for syncing multiple device attendance records at once
/// </summary>
public sealed class DeviceBatchSyncRequest
{
    /// <summary>
    /// List of attendance records to sync
    /// </summary>
    [Required]
    public List<DeviceSyncRequest> Records { get; init; } = new();
}
