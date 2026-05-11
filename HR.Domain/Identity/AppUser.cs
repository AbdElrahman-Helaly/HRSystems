namespace internalEmployee.Auth.Models;

public sealed class AppUser
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string? NationalId { get; set; }
    public required string PasswordHash { get; set; }
    public AppRole Role { get; set; } = AppRole.User;
    public bool IsMale { get; set; }
    public bool IsPending { get; set; } = false;
    public string? Email { get; set; }
    public string? FirstNameAr { get; set; }
    public string? MiddleNameAr { get; set; }
    public string? LastNameAr { get; set; }
    public string? FirstNameEn { get; set; }
    public string? MiddleNameEn { get; set; }
    public string? LastNameEn { get; set; }
    public string? PassportNumber { get; set; }
    public string? MachineCode { get; set; }
    public string? FingerprintKey { get; set; }
    public bool AllowMobileAttendanceFromAnyLocation { get; set; } = false;
    public int? NationalityId { get; set; }
    public Religion? Religion { get; set; }
    public string? EmployeeCode { get; set; }
    public int? BranchId { get; set; }
    public int? JobId { get; set; }
    public Guid? ManagerId { get; set; }
    public int? MaritalStatusId { get; set; }
    public string? AddressAr { get; set; }
    public string? AddressEn { get; set; }
    public int? EmploymentModeId { get; set; }
    public int? GovernorateId { get; set; }
    public int? CityId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? PhoneNumber { get; set; }
    public int? DepartmentId { get; set; }
    public string? JobTitle { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? ContractEndDate { get; set; }
    public decimal? GrossSalary { get; set; }
    public decimal? ShiftRate { get; set; }
    public decimal? HousingAllowance { get; set; }
    public decimal? MealAllowance { get; set; }
    public decimal? TransportationAllowance { get; set; }
    public decimal? InsuranceAllowance { get; set; }
    public decimal? OvertimeRate { get; set; }
    public decimal? InsuranceSalary { get; set; }
    public bool IsInsured { get; set; } = false;
    public int? InsuranceCompanyId { get; set; }
    public string? CompanyPhoneNumber { get; set; }
    public string? CompanyEmail { get; set; }
    public string? ImageUrl { get; set; }
    public DateOnly? Birthday { get; set; }
    public bool IsDisabled { get; set; } = false;
    public decimal? SickLeaveBalance { get; set; }
    public decimal? WorkEarningsTax { get; set; }
    public internalEmployee.Data.Entities.WorkType WorkType { get; set; } = internalEmployee.Data.Entities.WorkType.Onsite;
    public string? WorkFromHomeDays { get; set; }
    public internalEmployee.Data.Entities.EmployeeBankInfo? BankInfo { get; set; }
    public List<internalEmployee.Data.Entities.EmployeeEducation> Educations { get; set; } = new();
    public internalEmployee.Data.Entities.EmployeeWorkSchedule? WorkSchedule { get; set; }
}
