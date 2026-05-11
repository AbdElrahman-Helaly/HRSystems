namespace internalEmployee.Data.Entities;

public sealed class SalaryAdvance
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public decimal MonthlyDeduction { get; set; }
    public int NumberOfMonths { get; set; }
    public DateTime StartDate { get; set; }
    public string? Reason { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Guid? CreatedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
}
