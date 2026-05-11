namespace internalEmployee.Data.Entities;

public sealed class City
{
    public int Id { get; set; }
    public int GovernorateId { get; set; }
    public required string Name { get; set; }
    public string? NameAr { get; set; }
    public bool IsActive { get; set; } = true;
}

