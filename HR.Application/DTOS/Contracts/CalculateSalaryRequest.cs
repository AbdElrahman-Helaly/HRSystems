namespace internalEmployee.Auth.Contracts;

/// <summary>
/// Request model for salary calculation
/// </summary>
public sealed class CalculateSalaryRequest
{
    /// <summary>
    /// Employee ID
    /// </summary>
    public Guid EmployeeId { get; init; }

    /// <summary>
    /// Month (1-12)
    /// </summary>
    public int Month { get; init; }

    /// <summary>
    /// Year
    /// </summary>
    public int Year { get; init; }
}

