namespace internalEmployee.Auth.Contracts;

public sealed class AdminInfoResponse
{
    public Guid Id { get; init; }
    public string? NationalId { get; init; }
    
    // Arabic name fields
    public string? FirstNameAr { get; init; }
    public string? MiddleNameAr { get; init; }
    public string? LastNameAr { get; init; }
    
    // English name fields
    public string? FirstNameEn { get; init; }
    public string? MiddleNameEn { get; init; }
    public string? LastNameEn { get; init; }
    
    public string? Email { get; init; }
    public string Role { get; init; } = string.Empty;
    public int? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
}

