namespace internalEmployee.Data.Entities;

public sealed class Nationality
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? NameAr { get; set; }
}

