using System.ComponentModel.DataAnnotations;
namespace internalEmployee.Auth.Contracts;

public sealed class RegisterRequest
{
    public string? NationalId { get; init; }
    public string? PassportNumber { get; init; }

    [Required, MinLength(8)]
    public string Password { get; init; } = string.Empty;

    public bool IsMale { get; init; }
    
    // Arabic name fields
    public string? FirstNameAr { get; init; }
    public string? MiddleNameAr { get; init; }
    public string? LastNameAr { get; init; }
    
    // English name fields
    public string? FirstNameEn { get; init; }
    public string? MiddleNameEn { get; init; }
    public string? LastNameEn { get; init; }
    
    public string? MachineCode { get; init; }
    public string? FingerprintKey { get; init; }
    public bool AllowMobileAttendanceFromAnyLocation { get; init; }
    public int? NationalityId { get; init; }
    public internalEmployee.Auth.Models.Religion? Religion { get; init; }
    
    [Required]
    public string PhoneNumber { get; init; } = string.Empty;
    public int? DepartmentId { get; init; }
    public string? JobTitle { get; init; }
    public DateOnly? StartDate { get; init; }
    public bool HasCustody { get; init; }
    public List<string>? CustodyDetails { get; init; }
    [EmailAddress]
    public string? Email { get; init; }
    public string? CompanyPhoneNumber { get; init; }
    [EmailAddress]
    public string? CompanyEmail { get; init; }
    public DateOnly? Birthday { get; init; }
}


