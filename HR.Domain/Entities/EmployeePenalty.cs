namespace internalEmployee.Data.Entities;

public sealed class EmployeePenalty
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public PenaltyType PenaltyType { get; set; }
    public decimal? Days { get; set; }
    public decimal? Amount { get; set; }
    public DateOnly PenaltyDate { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Guid? CreatedBy { get; set; }
    public bool IsApplied { get; set; } = false;
    public int? AppliedMonth { get; set; }
    public int? AppliedYear { get; set; }
}
