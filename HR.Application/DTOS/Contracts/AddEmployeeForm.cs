using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class AddEmployeeForm
{
    // بيانات الهوية
    public string? NationalId { get; set; }
    public string? PassportNumber { get; set; }
    public string? EmployeeCode { get; set; }

    // كلمة المرور (مطلوبة)
    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    // بيانات الاسم (عربي)
    public string? FirstNameAr { get; set; }
    public string? MiddleNameAr { get; set; }
    public string? LastNameAr { get; set; }

    // بيانات الاسم (إنجليزي)
    public string? FirstNameEn { get; set; }
    public string? MiddleNameEn { get; set; }
    public string? LastNameEn { get; set; }

    // البيانات الأساسية
    public bool IsMale { get; set; }
    public int? NationalityId { get; set; }
    public internalEmployee.Auth.Models.Religion? Religion { get; set; }
    public int? MaritalStatusId { get; set; }
    public DateOnly? Birthday { get; set; }

    // بيانات الاتصال
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
    [EmailAddress]
    public string? Email { get; set; }
    public string? HomePhone { get; set; }
    public string? AddressAr { get; set; }
    public string? AddressEn { get; set; }
    public int? GovernorateId { get; set; }
    public int? CityId { get; set; }

    // بيانات العمل
    public int? DepartmentId { get; set; }
    public int? BranchId { get; set; }
    public int? JobId { get; set; }
    public string? ManagerId { get; set; }
    public int? EmploymentModeId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? ContractEndDate { get; set; }
    [Display(Name = "GrossSalary", Description = "الراتب الإجمالي")]
    public decimal? GrossSalary { get; set; }
    
    [Display(Name = "ShiftRate", Description = "سعر الشيفت")]
    public decimal? ShiftRate { get; set; }
    [Display(Name = "HousingAllowance", Description = "بدل السكن")]
    public decimal? HousingAllowance { get; set; }
    [Display(Name = "MealAllowance", Description = "بدل الوجبة")]
    public decimal? MealAllowance { get; set; }
    [Display(Name = "TransportationAllowance", Description = "بدل المواصلات")]
    public decimal? TransportationAllowance { get; set; }
    [Display(Name = "InsuranceAllowance", Description = "بدل تأميني")]
    public decimal? InsuranceAllowance { get; set; }
    [Display(Name = "OvertimeRate", Description = "معدل ساعات العمل الإضافية")]
    public decimal? OvertimeRate { get; set; }
    public decimal? InsuranceSalary { get; set; }
    public decimal? WorkEarningsTax { get; set; }
    public bool IsInsured { get; set; } = false;
    public int? InsuranceCompanyId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDisabled { get; set; } = false;
    public decimal? SickLeaveBalance { get; set; }

    public internalEmployee.Data.Entities.WorkType WorkType { get; set; } = internalEmployee.Data.Entities.WorkType.Onsite;
    public List<DayOfWeek>? WorkFromHomeDays { get; set; }

    // Work schedule (per-employee)
    // Based on EmploymentModeId:
    // - Full Time: default 09:00-17:00
    // - Part Time: PartTimeStart/PartTimeEnd required
    //   - PartTimeUseDefaultWeek = true: works Sunday-Thursday
    //   - PartTimeUseDefaultWeek = false: use PartTimeWorkDays
    public TimeOnly? PartTimeStart { get; set; }
    public TimeOnly? PartTimeEnd { get; set; }
    public bool PartTimeUseDefaultWeek { get; set; } = true;
    public List<DayOfWeek>? PartTimeWorkDays { get; set; }

    // بيانات الشركة
    public string? CompanyEmail { get; set; }
    public string? CompanyPhoneNumber { get; set; }

    // Bank Information
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? IbanNumber { get; set; }
    public string? SwiftBicCode { get; set; }
    public string? BankBranchCode { get; set; }

    // Education (multiple)
    public List<AddEmployeeEducationForm> Educations { get; set; } = new();

    // بيانات إضافية
    public string? MachineCode { get; set; }
    public string? FingerprintKey { get; set; }
    public bool AllowMobileAttendanceFromAnyLocation { get; set; } = false;

    // Role (optional) - defaults to User
    public internalEmployee.Auth.Models.AppRole? Role { get; set; }

    // صورة الموظف (اختيارية)
    public IFormFile? Image { get; set; }

    // الملفات المرفقة (attachments)
    public IFormFile[]? Attachments { get; set; }
}

public sealed class AddEmployeeEducationForm
{
    public string? UniversityName { get; set; }
    public DateOnly? GraduationYear { get; set; }
    public string? Degree { get; set; }
    public string? FinalGrade { get; set; }
}

