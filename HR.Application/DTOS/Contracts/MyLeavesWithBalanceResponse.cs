namespace internalEmployee.Auth.Contracts;

public sealed class MyLeavesWithBalanceResponse
{
    public required LeaveBalanceResponse Balance { get; init; }
    public required List<LeaveResponse> Leaves { get; init; }
}
