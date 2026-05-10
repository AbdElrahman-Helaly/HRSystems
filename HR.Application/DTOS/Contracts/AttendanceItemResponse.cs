namespace internalEmployee.Auth.Contracts;

public sealed class AttendanceItemResponse
{
    public int Id { get; init; }
    public DateOnly Date { get; init; }
    public TimeOnly AttendanceTime { get; init; }
    public DateTime CreatedAt { get; init; }
}

