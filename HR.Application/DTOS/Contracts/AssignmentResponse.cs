namespace internalEmployee.Auth.Contracts;

public sealed class AssignmentResponse
{
    public int Id { get; init; }
    public Guid UserId { get; init; }
    public string? EmployeeNameAr { get; init; }
    public string? EmployeeNameEn { get; init; }
    public required string Where { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public string? Reason { get; init; }
    public DateTime CreatedAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? RejectionReason { get; init; }
}
