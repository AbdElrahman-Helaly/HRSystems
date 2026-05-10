using System.ComponentModel.DataAnnotations;
using internalEmployee.Auth.Models;

namespace internalEmployee.Auth.Contracts;

public sealed class CreateAdminRequest
{
    public string? NationalId { get; init; }
    public string? PassportNumber { get; init; }

    [Required, MinLength(8)]
    public required string Password { get; init; }

    public AppRole Role { get; init; } = AppRole.Admin;
    [Required]
    public required string PhoneNumber { get; init; }
    
    // Arabic name fields
    public string? FirstNameAr { get; init; }
    public string? MiddleNameAr { get; init; }
    public string? LastNameAr { get; init; }
    
    // English name fields
    public string? FirstNameEn { get; init; }
    public string? MiddleNameEn { get; init; }
    public string? LastNameEn { get; init; }
    
    public string? Email { get; init; }
    public int? DepartmentId { get; init; }
}

