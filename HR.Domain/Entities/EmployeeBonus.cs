namespace internalEmployee.Data.Entities;

public sealed class EmployeeBonus
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly BonusDate { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Guid? CreatedBy { get; set; }
}
