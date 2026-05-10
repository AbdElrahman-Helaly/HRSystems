using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class UpdateEmployeeForm
{
    // بيانات الهوية
    [Display(Name = "NationalId", Description = "رقم الهوية الوطنية")]
    public string? NationalId { get; set; }
    
    [Display(Name = "PassportNumber", Description = "رقم جواز السفر")]
    public string? PassportNumber { get; set; }
    
    [Display(Name = "EmployeeCode", Description = "كود الموظف")]
    public string? EmployeeCode { get; set; }

    // بيانات الاسم (عربي)
    [Display(Name = "FirstNameAr", Description = "الاسم الأول (عربي)")]
    public string? FirstNameAr { get; set; }
    
    [Display(Name = "MiddleNameAr", Description = "الاسم الأوسط (عربي)")]
    public string? MiddleNameAr { get; set; }
    
    [Display(Name = "LastNameAr", Description = "اسم العائلة (عربي)")]
    public string? LastNameAr { get; set; }

    // بيانات الاسم (إنجليزي)
    [Display(Name = "FirstNameEn", Description = "الاسم الأول (إنجليزي)")]
    public string? FirstNameEn { get; set; }
    
    [Display(Name = "MiddleNameEn", Description = "الاسم الأوسط (إنجليزي)")]
    public string? MiddleNameEn { get; set; }
    
    [Display(Name = "LastNameEn", Description = "اسم العائلة (إنجليزي)")]
    public string? LastNameEn { get; set; }

    // البيانات الأساسية
    [Display(Name = "IsMale", Description = "الجنس (true = ذكر، false = أنثى)")]
    public bool? IsMale { get; set; }
    
    [Display(Name = "NationalityId", Description = "معرف الجنسية")]
    public int? NationalityId { get; set; }

    [Display(Name = "Religion", Description = "الديانة (1=Muslim, 2=Christian, 3=Other)")]
    public internalEmployee.Auth.Models.Religion? Religion { get; set; }
    
    [Display(Name = "MaritalStatusId", Description = "معرف الحالة الاجتماعية")]
    public int? MaritalStatusId { get; set; }
    
    [Display(Name = "Birthday", Description = "تاريخ الميلاد (YYYY-MM-DD)")]
    public DateOnly? Birthday { get; set; }

    // بيانات الاتصال
    [Display(Name = "PhoneNumber", Description = "رقم الهاتف")]
    public string? PhoneNumber { get; set; }
    
    [EmailAddress]
    [Display(Name = "Email", Description = "البريد الإلكتروني")]
    public string? Email { get; set; }
    
    [Display(Name = "HomePhone", Description = "هاتف المنزل")]
    public string? HomePhone { get; set; }
    
    [Display(Name = "AddressAr", Description = "العنوان (عربي)")]
    public string? AddressAr { get; set; }
    
    [Display(Name = "AddressEn", Description = "العنوان (إنجليزي)")]
    public string? AddressEn { get; set; }
    
    [Display(Name = "GovernorateId", Description = "معرف المحافظة")]
    public int? GovernorateId { get; set; }
    
    [Display(Name = "CityId", Description = "معرف المدينة")]
    public int? CityId { get; set; }

    // بيانات العمل
    [Display(Name = "DepartmentId", Description = "معرف القسم")]
    public int? DepartmentId { get; set; }
    
    [Display(Name = "BranchId", Description = "معرف الفرع")]
    public int? BranchId { get; set; }
    
    [Display(Name = "JobId", Description = "معرف الوظيفة")]
    public int? JobId { get; set; }
    
    [Display(Name = "ManagerId", Description = "معرف المدير (GUID)")]
    public string? ManagerId { get; set; }
    
    [Display(Name = "EmploymentModeId", Description = "معرف نمط التوظيف")]
    public int? EmploymentModeId { get; set; }
    
    [Display(Name = "StartDate", Description = "تاريخ البدء (YYYY-MM-DD)")]
    public DateOnly? StartDate { get; set; }
    
    [Display(Name = "ContractEndDate", Description = "تاريخ انتهاء العقد (YYYY-MM-DD)")]
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
    
    [Display(Name = "InsuranceSalary", Description = "الراتب التأميني")]
    public decimal? InsuranceSalary { get; set; }
    public decimal? WorkEarningsTax { get; set; }
    
    [Display(Name = "IsInsured", Description = "مؤمن عليه")]
    public bool? IsInsured { get; set; }
    
    [Display(Name = "InsuranceCompanyId", Description = "معرف شركة التأمين")]
    public int? InsuranceCompanyId { get; set; }
    
    [Display(Name = "IsActive", Description = "حالة الموظف (true = نشط، false = غير نشط)")]
    public bool? IsActive { get; set; }
    
    [Display(Name = "IsDisabled", Description = "حالة الإعاقة (true = معاق، false = غير معاق)")]
    public bool? IsDisabled { get; set; }
    
    [Display(Name = "SickLeaveBalance", Description = "رصيد الإجازة المرضية")]
    public decimal? SickLeaveBalance { get; set; }

    [Display(Name = "WorkType", Description = "نوع العمل (1=Onsite, 2=Remote, 3=Hybrid)")]
    public internalEmployee.Data.Entities.WorkType? WorkType { get; set; }

    [Display(Name = "WorkFromHomeDays", Description = "أيام العمل من المنزل (Hybrid only)")]
    public List<DayOfWeek>? WorkFromHomeDays { get; set; }

    // Work schedule (per-employee)
    // Based on EmploymentModeId:
    // - Full Time: default 09:00-17:00
    // - Part Time: PartTimeStart/PartTimeEnd required
    //   - PartTimeUseDefaultWeek = true: works Sunday-Thursday
    //   - PartTimeUseDefaultWeek = false: use PartTimeWorkDays
    [Display(Name = "PartTimeStart", Description = "وقت بدء العمل الجزئي (HH:mm)")]
    public TimeOnly? PartTimeStart { get; set; }
    
    [Display(Name = "PartTimeEnd", Description = "وقت انتهاء العمل الجزئي (HH:mm)")]
    public TimeOnly? PartTimeEnd { get; set; }

    [Display(Name = "PartTimeUseDefaultWeek", Description = "true = الأحد للخميس, false = حسب الأيام المختارة")]
    public bool? PartTimeUseDefaultWeek { get; set; }

    [Display(Name = "PartTimeWorkDays", Description = "أيام عمل البارت تايم عند PartTimeUseDefaultWeek=false")]
    public List<DayOfWeek>? PartTimeWorkDays { get; set; }
    
    // بيانات الشركة
    [EmailAddress]
    [Display(Name = "CompanyEmail", Description = "البريد الإلكتروني للشركة")]
    public string? CompanyEmail { get; set; }
    
    [Display(Name = "CompanyPhoneNumber", Description = "رقم هاتف الشركة")]
    public string? CompanyPhoneNumber { get; set; }

    // Bank Information
    [Display(Name = "BankName", Description = "اسم البنك")]
    public string? BankName { get; set; }
    
    [Display(Name = "AccountNumber", Description = "رقم الحساب")]
    public string? AccountNumber { get; set; }
    
    [Display(Name = "IbanNumber", Description = "رقم IBAN")]
    public string? IbanNumber { get; set; }
    
    [Display(Name = "SwiftBicCode", Description = "رمز SWIFT/BIC")]
    public string? SwiftBicCode { get; set; }
    
    [Display(Name = "BankBranchCode", Description = "رمز فرع البنك")]
    public string? BankBranchCode { get; set; }

    // Education (multiple) - لو فيه Id يعدل، لو مفيش يضيف
    [Display(Name = "Educations", Description = "التحصيل العلمي (JSON array)")]
    public List<UpdateEmployeeEducationForm>? Educations { get; set; }

    // بيانات إضافية
    [Display(Name = "MachineCode", Description = "كود الجهاز")]
    public string? MachineCode { get; set; }
    
    [Display(Name = "FingerprintKey", Description = "مفتاح البصمة")]
    public string? FingerprintKey { get; set; }

    [Display(Name = "AllowMobileAttendanceFromAnyLocation", Description = "السماح بالبصمة من أي مكان عبر الموبايل")]
    public bool? AllowMobileAttendanceFromAnyLocation { get; set; }

    // صورة الموظف (اختيارية)
    [Display(Name = "Image", Description = "صورة الموظف")]
    public IFormFile? Image { get; set; }

    // الملفات المرفقة (attachments) - إضافة ملفات جديدة فقط
    [Display(Name = "Attachments", Description = "الملفات المرفقة")]
    public IFormFile[]? Attachments { get; set; }
}

public sealed class UpdateEmployeeEducationForm
{
    public long? Id { get; set; } // لو موجود يعدل، لو مفيش يضيف جديد
    public string? UniversityName { get; set; }
    public DateOnly? GraduationYear { get; set; }
    public string? Degree { get; set; }
    public string? FinalGrade { get; set; }
}

