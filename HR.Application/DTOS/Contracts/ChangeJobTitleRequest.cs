namespace internalEmployee.Auth.Contracts;

public sealed class ChangeJobTitleRequest
{
    public int NewJobId { get; set; }
    public string? Reason { get; set; }
}
