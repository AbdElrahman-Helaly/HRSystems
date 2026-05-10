namespace internalEmployee.Auth.Contracts;

public sealed class EmployeeDetailsResponse
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
    public decimal? SickLeaveBalance { get; init; }
    public internalEmployee.Data.Entities.WorkType WorkType { get; init; }
    public List<DayOfWeek>? WorkFromHomeDays { get; init; }

    public string? PhoneNumber { get; init; }
    public int? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? ContractEndDate { get; init; }
    public decimal? GrossSalary { get; init; }
    public decimal? ShiftRate { get; init; }
    public decimal? HousingAllowance { get; init; }
    public decimal? MealAllowance { get; init; }
    public decimal? TransportationAllowance { get; init; }
    public decimal? InsuranceAllowance { get; init; }
    public decimal? OvertimeRate { get; init; }
    public decimal? WorkEarningsTax { get; init; }
    public decimal? InsuranceSalary { get; init; }
    public bool IsInsured { get; init; }
    public int? InsuranceCompanyId { get; init; }
    public string? InsuranceCompanyName { get; init; }

    // Schedule details (based on EmploymentModeId)
    public TimeOnly? PartTimeStart { get; init; }
    public TimeOnly? PartTimeEnd { get; init; }
    public bool PartTimeUseDefaultWeek { get; init; } = true;
    public List<DayOfWeek>? PartTimeWorkDays { get; init; }
    public string? CompanyPhoneNumber { get; init; }
    public string? CompanyEmail { get; init; }
    public string? ImageUrl { get; init; } // relative; controller can convert to absolute
    public string Role { get; init; } = string.Empty;
    public bool IsMale { get; init; }
    public bool IsPending { get; init; }
    public DateOnly? Birthday { get; init; }

    public EmployeeBankInfoResponse? BankInfo { get; init; }
    public List<EmployeeEducationResponse> Educations { get; init; } = new();
    public List<EmployeeAttachmentResponse> Attachments { get; init; } = new();
}

public sealed class EmployeeBankInfoResponse
{
    public string? BankName { get; init; }
    public string? AccountNumber { get; init; }
    public string? IbanNumber { get; init; }
    public string? SwiftBicCode { get; init; }
    public string? BranchCode { get; init; }
}

public sealed class EmployeeEducationResponse
{
    public long Id { get; init; }
    public string? UniversityName { get; init; }
    public DateOnly? GraduationYear { get; init; }
    public string? Degree { get; init; }
    public string? FinalGrade { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class EmployeeAttachmentResponse
{
    public Guid Id { get; init; }
    public string? OriginalFileName { get; init; }
    public string? ContentType { get; init; }
    public long FileSize { get; init; }
    public DateTime UploadedAt { get; init; }
    public string? FileUrl { get; set; } // set by controller (absolute URL)
    public string? FilePath { get; init; } // relative path from storage
}


