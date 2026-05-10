namespace internalEmployee.Auth.Contracts;

public sealed class OrganizationChartResponse
{
    public required OrganizationChartUser CEO { get; init; }
    public required List<DepartmentGroup> Departments { get; init; }
}

public sealed class DepartmentGroup
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public OrganizationChartUser? Manager { get; init; }
    public required List<OrganizationChartUser> Managers { get; init; }
    public required List<OrganizationChartUser> Employees { get; init; }
}

public sealed class OrganizationChartUser
{
    public Guid Id { get; init; }
    
    // Arabic name fields
    public string? FirstNameAr { get; init; }
    public string? MiddleNameAr { get; init; }
    public string? LastNameAr { get; init; }
    
    // English name fields
    public string? FirstNameEn { get; init; }
    public string? MiddleNameEn { get; init; }
    public string? LastNameEn { get; init; }
    
    public string? ImageUrl { get; init; }
    public required string Level { get; init; } // "CEO", "MANAGER", "EMPLOYEE"
}

