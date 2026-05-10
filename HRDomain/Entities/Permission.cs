namespace internalEmployee.Data.Entities;

public sealed class Permission
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime Date { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? RejectionReason { get; set; }
}

