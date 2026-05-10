namespace internalEmployee.Auth.Contracts;

public sealed class TransferDepartmentRequest
{
    public int NewDepartmentId { get; set; }
    public string? Reason { get; set; }
}
