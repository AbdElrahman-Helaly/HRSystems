namespace internalEmployee.Data.Entities;

public sealed class HealthInsuranceEnrollment
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public decimal MonthlyPremium { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Guid? CreatedBy { get; set; }
}
