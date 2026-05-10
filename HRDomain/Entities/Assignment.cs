namespace internalEmployee.Data.Entities;

public sealed class Assignment
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public required string Where { get; set; } // Location/place
    public DateTime StartDate { get; set; } // Date of the assignment
    public DateTime EndDate { get; set; } // End date of the assignment
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? RejectionReason { get; set; }
}

