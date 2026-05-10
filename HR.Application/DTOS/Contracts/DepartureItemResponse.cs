namespace internalEmployee.Auth.Contracts;

public sealed class DepartureItemResponse
{
    public int Id { get; init; }
    public DateOnly Date { get; init; }
    public TimeOnly DepartureTime { get; init; }
    public DateTime CreatedAt { get; init; }
}

