namespace internalEmployee.Auth.Contracts;

/// <summary>
/// Response model for device sync operation
/// </summary>
public sealed class DeviceSyncResponse
{
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
    /// Descriptive message about the operation result
    /// </summary>
    public required string Message { get; init; }
}
