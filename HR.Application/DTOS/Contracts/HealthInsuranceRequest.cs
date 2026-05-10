using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class HealthInsuranceRequest
{
    [Required]
    public Guid EmployeeId { get; init; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal MonthlyPremium { get; init; }

    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? Notes { get; init; }
}

public sealed class HealthInsuranceResponse
{
    public int Id { get; init; }
    public Guid UserId { get; init; }
    public decimal MonthlyPremium { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool IsActive { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
}
