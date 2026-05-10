namespace internalEmployee.Auth.Contracts;

/// <summary>
/// Represents dashboard data for a department
/// </summary>
public sealed class DepartmentDashboardItem
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
    /// Employees in this department with their attendance times
    /// </summary>
    public required List<AdminUserItem> Employees { get; init; }

    /// <summary>
    /// All requests from employees in this department
    /// </summary>
    public required List<RequestItem> Requests { get; init; }
}
