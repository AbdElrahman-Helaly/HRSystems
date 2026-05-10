using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class SalaryAdvanceRequest
{
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; init; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal MonthlyDeduction { get; init; }

    public DateOnly? StartDate { get; init; } // Defaults to today if not provided

    public string? Reason { get; init; }
}

public sealed class SalaryAdvanceManualRequest
{
    [Required]
    public Guid EmployeeId { get; init; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; init; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal MonthlyDeduction { get; init; }

    public DateOnly? StartDate { get; init; }

    public string? Reason { get; init; }
}

public sealed class SalaryAdvanceResponse
{
    public int Id { get; init; }
    public Guid UserId { get; init; }
    public decimal Amount { get; init; }
    public decimal MonthlyDeduction { get; init; }
    public int NumberOfMonths { get; init; }
    public DateOnly StartDate { get; init; }
    public string? Reason { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? RejectionReason { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ApprovedAt { get; init; }
}
