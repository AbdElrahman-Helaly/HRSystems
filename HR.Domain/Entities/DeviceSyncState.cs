namespace internalEmployee.Data.Entities;

public sealed class DeviceSyncState
{
    public required string DeviceKey { get; set; }
    public DateOnly? LastSyncedDate { get; set; }
    public DateTime? LastSyncedAt { get; set; }
}
