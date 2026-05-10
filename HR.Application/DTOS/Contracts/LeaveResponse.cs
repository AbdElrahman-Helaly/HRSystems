namespace internalEmployee.Auth.Contracts;

public sealed class LeaveResponse
{
    public int Id { get; init; }
    public Guid UserId { get; init; }
    public string? EmployeeNameAr { get; init; }
    public string? EmployeeNameEn { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public string? Reason { get; init; }
    public DateTime CreatedAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? RejectionReason { get; init; }
    public string LeaveType { get; init; } = string.Empty;
    public string? MedicalReportUrl { get; init; }
}

