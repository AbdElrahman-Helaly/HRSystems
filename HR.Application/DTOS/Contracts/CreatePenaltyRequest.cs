using internalEmployee.Data.Entities;

namespace internalEmployee.Auth.Contracts;

public sealed class CreatePenaltyRequest
{
    public Guid UserId { get; set; }
    public PenaltyType PenaltyType { get; set; }
    public decimal? Days { get; set; }
    public decimal? Amount { get; set; }
    public DateOnly PenaltyDate { get; set; }
    public string? Reason { get; set; }
}

public sealed class UpdatePenaltyRequest
{
    public PenaltyType? PenaltyType { get; set; }
    public decimal? Days { get; set; }
    public decimal? Amount { get; set; }
    public DateOnly? PenaltyDate { get; set; }
    public string? Reason { get; set; }
}
