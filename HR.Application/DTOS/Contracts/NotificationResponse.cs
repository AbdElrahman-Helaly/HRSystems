namespace internalEmployee.Auth.Contracts;

public sealed class NotificationResponse
{
    public int Id { get; init; }
    public Guid UserId { get; init; }
    public string Type { get; init; } = string.Empty;
    public int RequestId { get; init; }
    public required string Message { get; init; }
    public bool IsRead { get; init; }
    public bool IsConfirmed { get; init; }
    public DateTime? ConfirmedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public Guid? RequesterUserId { get; init; }
    public string? RequesterName { get; init; }
}

