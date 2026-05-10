using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

/// <summary>
/// Request model for updating sick leave balance
/// </summary>
public sealed class UpdateSickLeaveBalanceRequest
{
    /// <summary>
    /// Employee ID
    /// </summary>
    [Required]
    public Guid EmployeeId { get; init; }

    /// <summary>
    /// Sick leave balance (number of days)
    /// </summary>
    [Required]
    [Range(0, 1000, ErrorMessage = "Sick leave balance must be between 0 and 1000 days.")]
    public decimal SickLeaveBalance { get; init; }

    /// <summary>
    /// Start date for the sick leave balance period
    /// </summary>
    [Required]
    public DateOnly StartDate { get; init; }

    /// <summary>
    /// End date for the sick leave balance period
    /// </summary>
    [Required]
    public DateOnly EndDate { get; init; }
}

