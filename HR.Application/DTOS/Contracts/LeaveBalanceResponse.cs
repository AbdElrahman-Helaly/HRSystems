namespace internalEmployee.Auth.Contracts;

/// <summary>
/// Response model for leave balance calculation
/// </summary>
public sealed class LeaveBalanceResponse
{
    /// <summary>
    /// Annual leave balance (eligible days)
    /// </summary>
    public decimal AnnualLeaveBalance { get; init; }

    /// <summary>
    /// Annual leave days used (Annual + Casual)
    /// </summary>
    public decimal AnnualLeaveUsed { get; init; }

    /// <summary>
    /// Remaining annual leave balance
    /// </summary>
    public decimal AnnualLeaveRemaining { get; init; }

    /// <summary>
    /// Casual leave days used (max 6 per year)
    /// </summary>
    public decimal CasualLeaveUsed { get; init; }

    /// <summary>
    /// Sick leave balance (separate from annual)
    /// </summary>
    public decimal SickLeaveBalance { get; init; }

    /// <summary>
    /// Sick leave days used
    /// </summary>
    public decimal SickLeaveUsed { get; init; }

    /// <summary>
    /// Maternity leave count (max 3 times)
    /// </summary>
    public int Maternity { get; init; }

    /// <summary>
    /// Paternity leave count
    /// </summary>
    public int Paternity { get; init; }

    /// <summary>
    /// Hajj leave count (max 1 time, after 5 years)
    /// </summary>
    public int Hajj { get; init; }

    /// <summary>
    /// Exam leave count
    /// </summary>
    public int Exam { get; init; }
}

