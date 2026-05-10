namespace internalEmployee.Data.Entities;

public sealed class EmployeeCustody
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int CustodyItemId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}
