namespace internalEmployee.Auth.Contracts;

public sealed class CreateBonusRequest
{
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly BonusDate { get; set; }
    public string? Reason { get; set; }
}

public sealed class UpdateBonusRequest
{
    public decimal? Amount { get; set; }
    public DateOnly? BonusDate { get; set; }
    public string? Reason { get; set; }
}
