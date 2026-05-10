namespace internalEmployee.Auth.Contracts;

/// <summary>
/// Statistics for HR Home page
/// </summary>
public sealed class HRHomeStatistics
{
    /// <summary>
    /// Total number of active employees (Role = User)
    /// </summary>
    public int TotalEmployees { get; init; }

    /// <summary>
    /// Total number of departments
    /// </summary>
    public int TotalDepartments { get; init; }

    /// <summary>
    /// Number of employees per department
    /// </summary>
    public required List<DepartmentEmployeeCount> EmployeesPerDepartment { get; init; }
}

