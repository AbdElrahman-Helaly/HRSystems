using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class UpdateProfileRequest
{
    public string? Email { get; init; }
    
    // Arabic name fields
    public string? FirstNameAr { get; init; }
    public string? MiddleNameAr { get; init; }
    public string? LastNameAr { get; init; }
    
    // English name fields
    public string? FirstNameEn { get; init; }
    public string? MiddleNameEn { get; init; }
    public string? LastNameEn { get; init; }
    
    public int? MaritalStatusId { get; init; }
    public string? AddressAr { get; init; }
    public string? AddressEn { get; init; }
    public int? GovernorateId { get; init; }
    public int? CityId { get; init; }
    
    public DateOnly? Birthday { get; init; }
}

