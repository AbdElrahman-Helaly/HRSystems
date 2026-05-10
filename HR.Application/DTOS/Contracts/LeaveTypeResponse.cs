namespace internalEmployee.Auth.Contracts;

public sealed class LeaveTypeResponse
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string NameAr { get; init; }
}
