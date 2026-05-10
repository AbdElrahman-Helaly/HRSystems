namespace internalEmployee.Auth.Contracts;

public sealed class StatusUpdateResponse
{
    public required int Id { get; init; }
    public required string Status { get; init; }
    public string? RejectionReason { get; init; }
}

