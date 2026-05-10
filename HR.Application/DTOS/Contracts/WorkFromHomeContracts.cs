using internalEmployee.Data.Entities;

namespace internalEmployee.Auth.Contracts;

public class WorkFromHomeCreateRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
}

public class WorkFromHomeResponse
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string? EmployeeName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RejectionReason { get; set; }
}
