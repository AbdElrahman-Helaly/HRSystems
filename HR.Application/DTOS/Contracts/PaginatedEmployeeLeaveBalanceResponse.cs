using internalEmployee.Auth.Contracts;

namespace internalEmployee.Auth.Contracts;

/// <summary>
/// Paginated response for employee leave balances
/// </summary>
public sealed class PaginatedEmployeeLeaveBalanceResponse
{
    /// <summary>
    /// List of employee leave balances
    /// </summary>
    public List<EmployeeLeaveBalanceResponse> Items { get; init; } = new();

    /// <summary>
    /// Current page number
    /// </summary>
    public int PageNumber { get; init; }

    /// <summary>
    /// Items per page
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of items (before pagination)
    /// </summary>
    public int TotalCount { get; init; }
}
