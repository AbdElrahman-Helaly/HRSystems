namespace internalEmployee.Auth.Contracts;

/// <summary>
/// Represents the number of employees in a department
/// </summary>
public sealed class DepartmentEmployeeCount
{
    /// <summary>
    /// Department ID
    /// </summary>
    public int DepartmentId { get; init; }

    /// <summary>
    /// Department name
    /// </summary>
    public required string DepartmentName { get; init; }

    /// <summary>
    /// Number of employees in this department
    /// </summary>
    public int EmployeeCount { get; init; }
}

