namespace internalEmployee.Auth.Contracts;

public sealed class DeviceIncrementalSyncResponse
{
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public int TotalDeviceLogs { get; init; }
    public int SavedCount { get; init; }
    public string Message { get; init; } = string.Empty;
}
