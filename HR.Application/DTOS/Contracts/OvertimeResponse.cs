namespace internalEmployee.Auth.Contracts;

public sealed class OvertimeResponse
{
    public int Id { get; init; }
    public Guid UserId { get; init; }
    public DateOnly Date { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public decimal TotalHours { get; init; }
    public decimal HourlyRate { get; init; }
    public decimal Amount { get; init; }
    public required string Reason { get; init; }
    public DateTime CreatedAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? RejectionReason { get; init; }
}
