namespace internalEmployee.Data.Entities;

public sealed class Leave
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? RejectionReason { get; set; }
    public LeaveType? LeaveType { get; set; }
    public string? MedicalReportUrl { get; set; }
}

