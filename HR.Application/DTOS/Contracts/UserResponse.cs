namespace internalEmployee.Auth.Contracts;

public sealed class UserResponse
{
    public Guid Id { get; init; }
    public string? NationalId { get; init; }
    public string? PassportNumber { get; init; }
    public string? Email { get; init; }
    
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
    public string? NationalityName { get; init; }
    public internalEmployee.Auth.Models.Religion? Religion { get; init; }
    
    public string? EmployeeCode { get; init; }
    public int? BranchId { get; init; }
    public string? BranchName { get; init; }
    public int? JobId { get; init; }
    public string? JobTitleName { get; init; }
    public Guid? ManagerId { get; init; }
    public string? ManagerName { get; init; }
    public string? DepartmentManagerName { get; init; }
    public int? MaritalStatusId { get; init; }
    public string? MaritalStatusName { get; init; }
    public string? AddressAr { get; init; }
    public string? AddressEn { get; init; }
    public int? EmploymentModeId { get; init; }
    public string? EmploymentModeName { get; init; }
    public int? GovernorateId { get; init; }
    public string? GovernorateName { get; init; }
    public int? CityId { get; init; }
    public string? CityName { get; init; }
    public bool IsActive { get; init; }
    public bool IsDisabled { get; init; }
    public decimal? WorkEarningsTax { get; init; }
    public internalEmployee.Data.Entities.WorkType WorkType { get; init; }
    public List<DayOfWeek>? WorkFromHomeDays { get; init; }
    
    public string? PhoneNumber { get; init; }
    public int? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public string? JobTitle { get; init; }
    public DateOnly? StartDate { get; init; }
    public string? CompanyPhoneNumber { get; init; }
    public string? CompanyEmail { get; init; }
    public string? ImageUrl { get; init; } // relative; controller can convert to absolute
    public string Role { get; init; } = string.Empty;
    public bool IsMale { get; init; }
    public bool IsPending { get; init; }
    public DateOnly? Birthday { get; init; }
}

