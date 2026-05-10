namespace internalEmployee.Data.Entities;

public sealed class JobTitle
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? NameAr { get; set; }
    public int? JobLevel { get; set; }
    public bool IsManagerRole { get; set; }
    public int? ParentJobId { get; set; }
}

