namespace internalEmployee.Auth.Contracts;

/// <summary>
/// Response model for individual sync item result
/// </summary>
public sealed class DeviceSyncItemResponse
{
    /// <summary>
    /// Machine code (Employee code) from ZKTeco device
    /// </summary>
    public required string MachineCode { get; init; }

    /// <summary>
    /// Date of attendance
    /// </summary>
    public DateOnly Date { get; init; }

    /// <summary>
    /// Indicates if a new record was created
    /// </summary>
    public bool IsNew { get; init; }

    /// <summary>
    /// Indicates if an existing record was updated
    /// </summary>
    public bool IsUpdated { get; init; }

    /// <summary>
    /// Indicates if the record was skipped (already exists with same data)
    /// </summary>
    public bool IsSkipped { get; init; }

    /// <summary>
    /// Indicates if MachineCode was not found
    /// </summary>
    public bool IsNotFound { get; init; }

    /// <summary>
    /// Descriptive message about the operation result
    /// </summary>
    public required string Message { get; init; }
}
