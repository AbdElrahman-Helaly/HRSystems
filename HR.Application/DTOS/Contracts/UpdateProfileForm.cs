using Microsoft.AspNetCore.Http;

namespace internalEmployee.Auth.Contracts;

public class UpdateProfileForm
{
    public string? Email { get; set; }
    
    // Arabic name fields
    public string? FirstNameAr { get; set; }
    public string? MiddleNameAr { get; set; }
    public string? LastNameAr { get; set; }
    
    // English name fields
    public string? FirstNameEn { get; set; }
    public string? MiddleNameEn { get; set; }
    public string? LastNameEn { get; set; }
    
    public int? MaritalStatusId { get; set; }
    public string? AddressAr { get; set; }
    public string? AddressEn { get; set; }
    public int? GovernorateId { get; set; }
    public int? CityId { get; set; }
    
    public string? Birthday { get; set; }
    public IFormFile? Image { get; set; }
}

