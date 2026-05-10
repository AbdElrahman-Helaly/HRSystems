namespace internalEmployee.Auth.Contracts;

/// <summary>
/// Response model for employee with leave balance
/// </summary>
public sealed class EmployeeLeaveBalanceResponse
{
    /// <summary>
    /// Employee ID
    /// </summary>
    public Guid EmployeeId { get; init; }

    /// <summary>
    /// Employee name (Arabic or English)
    /// </summary>
    public string? EmployeeName { get; init; }

    /// <summary>
    /// Leave balance details
    /// </summary>
    public LeaveBalanceResponse LeaveBalance { get; init; } = new();
}

