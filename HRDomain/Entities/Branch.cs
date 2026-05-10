namespace internalEmployee.Data.Entities;

public sealed class Branch
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? NameAr { get; set; }
    public bool IsActive { get; set; } = true;
}

