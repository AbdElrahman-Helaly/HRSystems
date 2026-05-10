namespace internalEmployee.Data.Entities;

public sealed class Overtime
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime Date { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal TotalHours { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal Amount { get; set; }
    public required string Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? RejectionReason { get; set; }
}
