using internalEmployee.Auth;
using internalEmployee.Auth.Contracts;
using internalEmployee.Auth.Models;
using internalEmployee.Data;
using internalEmployee.Services.Attachment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.IO;
using internalEmployee.Data.Entities;
using System.Text.Json;
using internalEmployee.Services.Sms;

namespace internalEmployee.Services.Auth;

public sealed class AuthService : IAuthService
{
    private readonly JwtOptions _jwt;
    private readonly AppDbContext _db;
    private readonly IUserAttachmentService _attachmentService;
    private readonly CompanyLocationOptions _companyLocation;
    private readonly SmsOptions _sms;
    private readonly HttpClient _httpClient;

    public AuthService(
        IOptions<JwtOptions> jwtOptions,
        AppDbContext db,
        IConfiguration configuration,
        HttpClient httpClient,
        IUserAttachmentService attachmentService,
        IOptions<CompanyLocationOptions> companyLocationOptions,
        IOptions<SmsOptions> smsOptions)
    {
        _jwt = jwtOptions.Value;
        _db = db;
        _httpClient = httpClient;
        _attachmentService = attachmentService;
        _companyLocation = companyLocationOptions.Value;
        _sms = smsOptions.Value;
        if (string.IsNullOrWhiteSpace(_jwt.SigningKey))
            throw new InvalidOperationException("Auth:Jwt:SigningKey is required.");

        SeedSuperAdminIfMissingAsync(configuration).GetAwaiter().GetResult();
        SeedHRIfMissingAsync(configuration).GetAwaiter().GetResult();
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var nationalId = request.NationalId?.Trim();
        var passportNumber = request.PassportNumber?.Trim();

        // Validate that at least one of NationalId or PassportNumber is provided
        if (string.IsNullOrWhiteSpace(nationalId) && string.IsNullOrWhiteSpace(passportNumber))
            throw new InvalidOperationException("Either NationalId or PassportNumber is required.");

        var phoneNumber = request.PhoneNumber.Trim();
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new InvalidOperationException("PhoneNumber is required.");

        // Check uniqueness of NationalId if provided
        if (!string.IsNullOrWhiteSpace(nationalId))
        {
        var nationalIdExists = await _db.Users
            .AnyAsync(u => u.NationalId == nationalId, ct);
        if (nationalIdExists)
            throw new InvalidOperationException("NationalId already registered.");
        }

        // Check uniqueness of PassportNumber if provided
        if (!string.IsNullOrWhiteSpace(passportNumber))
        {
            var passportNumberExists = await _db.Users
                .AnyAsync(u => u.PassportNumber == passportNumber, ct);
            if (passportNumberExists)
                throw new InvalidOperationException("PassportNumber already registered.");
        }

        var phoneNumberExists = await _db.Users
            .AnyAsync(u => u.PhoneNumber == phoneNumber, ct);
        if (phoneNumberExists)
            throw new InvalidOperationException("PhoneNumber already registered.");

        // Validate NationalityId if provided
        if (request.NationalityId.HasValue)
        {
            var nationalityExists = await _db.Nationalities
                .AnyAsync(n => n.Id == request.NationalityId.Value, ct);
            if (!nationalityExists)
                throw new InvalidOperationException("Invalid NationalityId.");
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            NationalId = nationalId,
            PassportNumber = passportNumber,
            PasswordHash = HashPassword(request.Password),
            Role = AppRole.User,
            IsMale = request.IsMale,
            IsPending = false,
            Email = request.Email?.Trim(),
            FirstNameAr = request.FirstNameAr?.Trim(),
            MiddleNameAr = request.MiddleNameAr?.Trim(),
            LastNameAr = request.LastNameAr?.Trim(),
            FirstNameEn = request.FirstNameEn?.Trim(),
            MiddleNameEn = request.MiddleNameEn?.Trim(),
            LastNameEn = request.LastNameEn?.Trim(),
            MachineCode = request.MachineCode?.Trim(),
            FingerprintKey = request.FingerprintKey?.Trim(),
            AllowMobileAttendanceFromAnyLocation = request.AllowMobileAttendanceFromAnyLocation,
            NationalityId = request.NationalityId,
            Religion = request.Religion,
            PhoneNumber = phoneNumber,
            DepartmentId = request.DepartmentId,
            JobTitle = request.JobTitle?.Trim(),
            StartDate = request.StartDate,
            CompanyPhoneNumber = request.CompanyPhoneNumber?.Trim(),
            CompanyEmail = request.CompanyEmail?.Trim(),
            Birthday = request.Birthday
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        // Assign default company location to newly registered user if configured
        if (_companyLocation.Latitude != 0 || _companyLocation.Longitude != 0)
        {
            var userLocation = new internalEmployee.Data.Entities.UserLocation
            {
                UserId = user.Id,
                Name = _companyLocation.Name,
                Latitude = _companyLocation.Latitude,
                Longitude = _companyLocation.Longitude,
                RadiusMeters = _companyLocation.RadiusMeters,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _db.UserLocations.Add(userLocation);
            await _db.SaveChangesAsync(ct);
        }

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var phoneNumber = request.PhoneNumber.Trim();
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new InvalidOperationException("PhoneNumber is required.");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, ct);

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            throw new InvalidOperationException("Invalid phone number or password.");

        return CreateAuthResponse(user);
    }

    public async Task<AppUser?> GetUserByNationalIdAsync(string nationalId, CancellationToken ct = default)
    {
        var normalized = nationalId?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        return await _db.Users
            .FirstOrDefaultAsync(u => u.NationalId == normalized, ct);
    }

    public AppUser? GetUserByNationalId(string nationalId)
    {
        // Synchronous version for backward compatibility
        return GetUserByNationalIdAsync(nationalId).GetAwaiter().GetResult();
    }

    public async Task<List<AppUser>> GetAllUsersAsync(CancellationToken ct)
    {
        return await _db.Users
            .OrderBy(u => u.FirstNameEn ?? u.FirstNameAr ?? u.NationalId ?? u.PassportNumber)
            .ToListAsync(ct);
    }

    public async Task<AppUser?> GetUserByIdAsync(Guid userId, CancellationToken ct)
    {
        return await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
    }

    public async Task<PaginatedResponse<UserResponse>> GetAllUsersWithDetailsAsync(
        int pageNumber, int pageSize, string? searchQuery, 
        int? departmentId, int? branchId, int? jobId, bool? isActive, 
        CancellationToken ct)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var search = searchQuery.Trim().ToLower();
            query = query.Where(u => 
                (u.FirstNameAr != null && u.FirstNameAr.ToLower().Contains(search)) ||
                (u.FirstNameEn != null && u.FirstNameEn.ToLower().Contains(search)) ||
                (u.MiddleNameAr != null && u.MiddleNameAr.ToLower().Contains(search)) ||
                (u.MiddleNameEn != null && u.MiddleNameEn.ToLower().Contains(search)) ||
                (u.LastNameAr != null && u.LastNameAr.ToLower().Contains(search)) ||
                (u.LastNameEn != null && u.LastNameEn.ToLower().Contains(search)) ||
                (u.Email != null && u.Email.ToLower().Contains(search)) ||
                (u.PhoneNumber != null && u.PhoneNumber.ToLower().Contains(search)) ||
                (u.EmployeeCode != null && u.EmployeeCode.ToLower().Contains(search)) ||
                (u.NationalId != null && u.NationalId.ToLower().Contains(search))
            );
        }

        if (departmentId.HasValue)
            query = query.Where(u => u.DepartmentId == departmentId.Value);

        if (branchId.HasValue)
            query = query.Where(u => u.BranchId == branchId.Value);

        if (jobId.HasValue)
            query = query.Where(u => u.JobId == jobId.Value);

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        var totalCount = await query.CountAsync(ct);

        var users = await query
            .OrderBy(u => u.FirstNameEn ?? u.FirstNameAr ?? u.NationalId ?? u.PassportNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var responses = new List<UserResponse>();

        foreach (var user in users)
        {
            var response = await MapUserToUserResponseAsync(user, ct);
            responses.Add(response);
        }

        return new PaginatedResponse<UserResponse>
        {
            Items = responses,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<List<EmployeeDetailsResponse>> GetAllEmployeesWithDetailsAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var employees = await _db.Users
            .AsNoTracking()
            .Where(u => u.Role == AppRole.User)
            .Include(u => u.BankInfo)
            .Include(u => u.Educations)
            .Include(u => u.WorkSchedule)
            .OrderBy(u => u.FirstNameEn ?? u.FirstNameAr ?? u.NationalId ?? u.PassportNumber)
            .ToListAsync(ct);

        var responses = new List<EmployeeDetailsResponse>(employees.Count);
        foreach (var user in employees)
        {
            // reuse existing mapper (by id) to include lookup names + attachments
            var full = await GetEmployeeByIdWithDetailsAsync(user.Id, ct);
            if (full != null) responses.Add(full);
        }

        return responses;
    }

    public async Task<UserResponse?> GetUserWithDetailsAsync(Guid userId, CancellationToken ct)
    {
        var user = await GetUserByIdAsync(userId, ct);
        if (user == null)
            return null;

        return await MapUserToUserResponseAsync(user, ct);
    }

    public async Task<UserProfileResponse?> GetUserProfileWithDetailsAsync(Guid userId, CancellationToken ct)
    {
        var user = await GetUserByIdAsync(userId, ct);
        if (user == null)
            return null;

        return await MapUserToUserProfileResponseAsync(user, ct);
    }

    public async Task<EmployeeDetailsResponse?> GetEmployeeByIdWithDetailsAsync(Guid employeeId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Use fresh query with explicit includes to ensure all related data is loaded
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.BankInfo)
            .Include(u => u.Educations.OrderByDescending(e => e.CreatedAt))
            .Include(u => u.WorkSchedule)
            .FirstOrDefaultAsync(u => u.Id == employeeId, ct);

        if (user == null)
            return null;

        // Lookup names (same logic as profile mapping)
        string? departmentName = null;
        if (user.DepartmentId.HasValue)
        {
            var department = await _db.Departments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == user.DepartmentId.Value, ct);
            departmentName = department?.Name;
        }

        string? nationalityName = null;
        if (user.NationalityId.HasValue)
        {
            var nationality = await _db.Nationalities
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == user.NationalityId.Value, ct);
            nationalityName = nationality?.Name;
        }

        string? branchName = null;
        if (user.BranchId.HasValue)
        {
            var branch = await _db.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == user.BranchId.Value, ct);
            branchName = branch?.Name;
        }

        string? jobTitleName = null;
        if (user.JobId.HasValue)
        {
            var jobTitle = await _db.JobTitles
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == user.JobId.Value, ct);
            jobTitleName = jobTitle?.Name;
        }

        string? managerName = null;
        string? departmentManagerName = await GetDepartmentManagerNameAsync(user.DepartmentId, ct);
        if (user.ManagerId.HasValue)
        {
            var manager = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == user.ManagerId.Value, ct);
            managerName = manager != null ? GetFullName(manager) : null;
        }

        string? maritalStatusName = null;
        if (user.MaritalStatusId.HasValue)
        {
            var maritalStatus = await _db.MaritalStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == user.MaritalStatusId.Value, ct);
            maritalStatusName = maritalStatus?.Name;
        }

        string? employmentModeName = null;
        if (user.EmploymentModeId.HasValue)
        {
            var employmentMode = await _db.EmploymentModes
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == user.EmploymentModeId.Value, ct);
            employmentModeName = employmentMode?.Name;
        }

        string? governorateName = null;
        if (user.GovernorateId.HasValue)
        {
            var governorate = await _db.Governorates
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == user.GovernorateId.Value, ct);
            governorateName = governorate?.Name;
        }

        string? cityName = null;
        if (user.CityId.HasValue)
        {
            var city = await _db.Cities
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == user.CityId.Value, ct);
            cityName = city?.Name;
        }

        string? insuranceCompanyName = null;
        if (user.InsuranceCompanyId.HasValue)
        {
            var insuranceCompany = await _db.InsuranceCompanies
                .AsNoTracking()
                .FirstOrDefaultAsync(ic => ic.Id == user.InsuranceCompanyId.Value, ct);
            insuranceCompanyName = insuranceCompany?.Name;
        }

        var attachments = await _attachmentService.GetUserAttachmentsAsync(user.Id, ct);

        return new EmployeeDetailsResponse
        {
            Id = user.Id,
            NationalId = user.NationalId,
            PassportNumber = user.PassportNumber,
            Email = user.Email,
            FirstNameAr = user.FirstNameAr,
            MiddleNameAr = user.MiddleNameAr,
            LastNameAr = user.LastNameAr,
            FirstNameEn = user.FirstNameEn,
            MiddleNameEn = user.MiddleNameEn,
            LastNameEn = user.LastNameEn,
            MachineCode = user.MachineCode,
            FingerprintKey = user.FingerprintKey,
            AllowMobileAttendanceFromAnyLocation = user.AllowMobileAttendanceFromAnyLocation,
            NationalityId = user.NationalityId,
            NationalityName = nationalityName,
            Religion = user.Religion,
            EmployeeCode = user.EmployeeCode,
            BranchId = user.BranchId,
            BranchName = branchName,
            JobId = user.JobId,
            JobTitleName = jobTitleName,
            ManagerId = user.ManagerId,
            ManagerName = managerName,
            DepartmentManagerName = departmentManagerName,
            MaritalStatusId = user.MaritalStatusId,
            MaritalStatusName = maritalStatusName,
            AddressAr = user.AddressAr,
            AddressEn = user.AddressEn,
            EmploymentModeId = user.EmploymentModeId,
            EmploymentModeName = employmentModeName,
            GovernorateId = user.GovernorateId,
            GovernorateName = governorateName,
            CityId = user.CityId,
            CityName = cityName,
            IsActive = user.IsActive,
            IsDisabled = user.IsDisabled,
            WorkType = user.WorkType,
            WorkFromHomeDays = user.WorkFromHomeDays != null ? JsonSerializer.Deserialize<List<DayOfWeek>>(user.WorkFromHomeDays) : null,
            PhoneNumber = user.PhoneNumber,
            DepartmentId = user.DepartmentId,
            DepartmentName = departmentName,
            StartDate = user.StartDate,
            ContractEndDate = user.ContractEndDate,
            GrossSalary = user.GrossSalary,
            ShiftRate = user.ShiftRate ?? 0m,
            HousingAllowance = user.HousingAllowance ?? 0m,
            MealAllowance = user.MealAllowance ?? 0m,
            TransportationAllowance = user.TransportationAllowance ?? 0m,
            InsuranceAllowance = user.InsuranceAllowance ?? 0m,
            OvertimeRate = user.OvertimeRate ?? 0m,
            InsuranceSalary = user.InsuranceSalary ?? 0m,
            IsInsured = user.IsInsured,
            InsuranceCompanyId = user.InsuranceCompanyId,
            InsuranceCompanyName = insuranceCompanyName,
            SickLeaveBalance = user.SickLeaveBalance,
            PartTimeStart = user.WorkSchedule?.PartTimeStart,
            PartTimeEnd = user.WorkSchedule?.PartTimeEnd,
            PartTimeUseDefaultWeek = user.WorkSchedule?.PartTimeUseDefaultWeek ?? true,
            PartTimeWorkDays = (user.WorkSchedule?.PartTimeUseDefaultWeek ?? true)
                ? null
                : DeserializePartTimeWorkDays(user.WorkSchedule?.PartTimeCustomDaysJson),
            CompanyPhoneNumber = user.CompanyPhoneNumber,
            CompanyEmail = user.CompanyEmail,
            ImageUrl = user.ImageUrl,
            Role = user.Role.ToString(),
            IsMale = user.IsMale,
            IsPending = user.IsPending,
            Birthday = user.Birthday,
            BankInfo = user.BankInfo == null
                ? null
                : new EmployeeBankInfoResponse
                {
                    BankName = user.BankInfo.BankName,
                    AccountNumber = user.BankInfo.AccountNumber,
                    IbanNumber = user.BankInfo.Iban,
                    SwiftBicCode = user.BankInfo.SwiftBic,
                    BranchCode = user.BankInfo.BranchCode
                },
            Educations = user.Educations
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new EmployeeEducationResponse
                {
                    Id = e.Id,
                    UniversityName = e.UniversityName,
                    GraduationYear = e.GraduationYear,
                    Degree = e.Degree,
                    FinalGrade = e.FinalGrade,
                    CreatedAt = e.CreatedAt
                })
                .ToList(),
            Attachments = attachments.Select(a => new EmployeeAttachmentResponse
            {
                Id = a.Id,
                OriginalFileName = a.OriginalFileName,
                ContentType = a.ContentType,
                FileSize = a.FileSize,
                UploadedAt = a.UploadedAt,
                FilePath = a.FilePath
            }).ToList()
        };
    }

    private async Task<UserResponse> MapUserToUserResponseAsync(AppUser user, CancellationToken ct)
    {
        string? departmentName = null;
        if (user.DepartmentId.HasValue)
        {
            var department = await _db.Departments
                .FirstOrDefaultAsync(d => d.Id == user.DepartmentId.Value, ct);
            departmentName = department?.Name;
        }

        string? nationalityName = null;
        if (user.NationalityId.HasValue)
        {
            var nationality = await _db.Nationalities
                .FirstOrDefaultAsync(n => n.Id == user.NationalityId.Value, ct);
            nationalityName = nationality?.Name;
        }

        string? branchName = null;
        if (user.BranchId.HasValue)
        {
            var branch = await _db.Branches
                .FirstOrDefaultAsync(b => b.Id == user.BranchId.Value, ct);
            branchName = branch?.Name;
        }

        string? jobTitleName = null;
        if (user.JobId.HasValue)
        {
            var jobTitle = await _db.JobTitles
                .FirstOrDefaultAsync(j => j.Id == user.JobId.Value, ct);
            jobTitleName = jobTitle?.Name;
        }

        string? managerName = null;
        string? departmentManagerName = await GetDepartmentManagerNameAsync(user.DepartmentId, ct);
        if (user.ManagerId.HasValue)
        {
            var manager = await _db.Users
                .FirstOrDefaultAsync(u => u.Id == user.ManagerId.Value, ct);
            managerName = manager != null ? GetFullName(manager) : null;
        }

        string? maritalStatusName = null;
        if (user.MaritalStatusId.HasValue)
        {
            var maritalStatus = await _db.MaritalStatuses
                .FirstOrDefaultAsync(m => m.Id == user.MaritalStatusId.Value, ct);
            maritalStatusName = maritalStatus?.Name;
        }

        string? employmentModeName = null;
        if (user.EmploymentModeId.HasValue)
        {
            var employmentMode = await _db.EmploymentModes
                .FirstOrDefaultAsync(e => e.Id == user.EmploymentModeId.Value, ct);
            employmentModeName = employmentMode?.Name;
        }

        string? governorateName = null;
        if (user.GovernorateId.HasValue)
        {
            var governorate = await _db.Governorates
                .FirstOrDefaultAsync(g => g.Id == user.GovernorateId.Value, ct);
            governorateName = governorate?.Name;
        }

        string? cityName = null;
        if (user.CityId.HasValue)
        {
            var city = await _db.Cities
                .FirstOrDefaultAsync(c => c.Id == user.CityId.Value, ct);
            cityName = city?.Name;
        }

        return new UserResponse
        {
            Id = user.Id,
            NationalId = user.NationalId,
            PassportNumber = user.PassportNumber,
            Email = user.Email,
            FirstNameAr = user.FirstNameAr,
            MiddleNameAr = user.MiddleNameAr,
            LastNameAr = user.LastNameAr,
            FirstNameEn = user.FirstNameEn,
            MiddleNameEn = user.MiddleNameEn,
            LastNameEn = user.LastNameEn,
            MachineCode = user.MachineCode,
            FingerprintKey = user.FingerprintKey,
            AllowMobileAttendanceFromAnyLocation = user.AllowMobileAttendanceFromAnyLocation,
            NationalityId = user.NationalityId,
            NationalityName = nationalityName,
            Religion = user.Religion,
            EmployeeCode = user.EmployeeCode,
            BranchId = user.BranchId,
            BranchName = branchName,
            JobId = user.JobId,
            JobTitleName = jobTitleName,
            ManagerId = user.ManagerId,
            ManagerName = managerName,
            DepartmentManagerName = departmentManagerName,
            MaritalStatusId = user.MaritalStatusId,
            MaritalStatusName = maritalStatusName,
            AddressAr = user.AddressAr,
            AddressEn = user.AddressEn,
            EmploymentModeId = user.EmploymentModeId,
            EmploymentModeName = employmentModeName,
            GovernorateId = user.GovernorateId,
            GovernorateName = governorateName,
            CityId = user.CityId,
            CityName = cityName,
            IsActive = user.IsActive,
            IsDisabled = user.IsDisabled,
            WorkEarningsTax = user.WorkEarningsTax,
            WorkType = user.WorkType,
            WorkFromHomeDays = user.WorkFromHomeDays != null ? JsonSerializer.Deserialize<List<DayOfWeek>>(user.WorkFromHomeDays) : null,
            PhoneNumber = user.PhoneNumber,
            DepartmentId = user.DepartmentId,
            DepartmentName = departmentName,
            JobTitle = user.JobTitle,
            StartDate = user.StartDate,
            CompanyPhoneNumber = user.CompanyPhoneNumber,
            CompanyEmail = user.CompanyEmail,
            ImageUrl = user.ImageUrl,
            Role = user.Role.ToString(),
            IsMale = user.IsMale,
            IsPending = user.IsPending,
            Birthday = user.Birthday
        };
    }

    private async Task<UserProfileResponse> MapUserToUserProfileResponseAsync(AppUser user, CancellationToken ct)
    {
        string? departmentName = null;
        if (user.DepartmentId.HasValue)
        {
            var department = await _db.Departments
                .FirstOrDefaultAsync(d => d.Id == user.DepartmentId.Value, ct);
            departmentName = department?.Name;
        }

        string? nationalityName = null;
        if (user.NationalityId.HasValue)
        {
            var nationality = await _db.Nationalities
                .FirstOrDefaultAsync(n => n.Id == user.NationalityId.Value, ct);
            nationalityName = nationality?.Name;
        }

        string? branchName = null;
        if (user.BranchId.HasValue)
        {
            var branch = await _db.Branches
                .FirstOrDefaultAsync(b => b.Id == user.BranchId.Value, ct);
            branchName = branch?.Name;
        }

        string? jobTitleName = null;
        if (user.JobId.HasValue)
        {
            var jobTitle = await _db.JobTitles
                .FirstOrDefaultAsync(j => j.Id == user.JobId.Value, ct);
            jobTitleName = jobTitle?.Name;
        }

        string? managerName = null;
        string? departmentManagerName = await GetDepartmentManagerNameAsync(user.DepartmentId, ct);
        if (user.ManagerId.HasValue)
        {
            var manager = await _db.Users
                .FirstOrDefaultAsync(u => u.Id == user.ManagerId.Value, ct);
            managerName = manager != null ? GetFullName(manager) : null;
        }

        string? maritalStatusName = null;
        if (user.MaritalStatusId.HasValue)
        {
            var maritalStatus = await _db.MaritalStatuses
                .FirstOrDefaultAsync(m => m.Id == user.MaritalStatusId.Value, ct);
            maritalStatusName = maritalStatus?.Name;
        }

        string? employmentModeName = null;
        if (user.EmploymentModeId.HasValue)
        {
            var employmentMode = await _db.EmploymentModes
                .FirstOrDefaultAsync(e => e.Id == user.EmploymentModeId.Value, ct);
            employmentModeName = employmentMode?.Name;
        }

        string? governorateName = null;
        if (user.GovernorateId.HasValue)
        {
            var governorate = await _db.Governorates
                .FirstOrDefaultAsync(g => g.Id == user.GovernorateId.Value, ct);
            governorateName = governorate?.Name;
        }

        string? cityName = null;
        if (user.CityId.HasValue)
        {
            var city = await _db.Cities
                .FirstOrDefaultAsync(c => c.Id == user.CityId.Value, ct);
            cityName = city?.Name;
        }

        return new UserProfileResponse
        {
            Id = user.Id,
            NationalId = user.NationalId,
            PassportNumber = user.PassportNumber,
            Email = user.Email,
            FirstNameAr = user.FirstNameAr,
            MiddleNameAr = user.MiddleNameAr,
            LastNameAr = user.LastNameAr,
            FirstNameEn = user.FirstNameEn,
            MiddleNameEn = user.MiddleNameEn,
            LastNameEn = user.LastNameEn,
            MachineCode = user.MachineCode,
            FingerprintKey = user.FingerprintKey,
            AllowMobileAttendanceFromAnyLocation = user.AllowMobileAttendanceFromAnyLocation,
            NationalityId = user.NationalityId,
            NationalityName = nationalityName,
            Religion = user.Religion,
            EmployeeCode = user.EmployeeCode,
            BranchId = user.BranchId,
            BranchName = branchName,
            JobId = user.JobId,
            JobTitleName = jobTitleName,
            ManagerId = user.ManagerId,
            ManagerName = managerName,
            DepartmentManagerName = departmentManagerName,
            MaritalStatusId = user.MaritalStatusId,
            MaritalStatusName = maritalStatusName,
            AddressAr = user.AddressAr,
            AddressEn = user.AddressEn,
            EmploymentModeId = user.EmploymentModeId,
            EmploymentModeName = employmentModeName,
            GovernorateId = user.GovernorateId,
            GovernorateName = governorateName,
            CityId = user.CityId,
            CityName = cityName,
            IsActive = user.IsActive,
            IsDisabled = user.IsDisabled,
            WorkEarningsTax = user.WorkEarningsTax,
            PhoneNumber = user.PhoneNumber,
            DepartmentId = user.DepartmentId,
            DepartmentName = departmentName,
            JobTitle = user.JobTitle,
            StartDate = user.StartDate,
            CompanyPhoneNumber = user.CompanyPhoneNumber,
            CompanyEmail = user.CompanyEmail,
            ImageUrl = user.ImageUrl,
            Role = user.Role.ToString(),
            IsMale = user.IsMale,
            IsPending = user.IsPending,
            Birthday = user.Birthday
        };
    }

    public async Task<AppUser> UpdateUserProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        
        if (user == null)
            throw new InvalidOperationException("User not found.");

        // Update only provided fields
        if (request.Email != null)
            user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        
        // Update Arabic name fields
        if (request.FirstNameAr != null)
            user.FirstNameAr = string.IsNullOrWhiteSpace(request.FirstNameAr) ? null : request.FirstNameAr.Trim();
        if (request.MiddleNameAr != null)
            user.MiddleNameAr = string.IsNullOrWhiteSpace(request.MiddleNameAr) ? null : request.MiddleNameAr.Trim();
        if (request.LastNameAr != null)
            user.LastNameAr = string.IsNullOrWhiteSpace(request.LastNameAr) ? null : request.LastNameAr.Trim();
        
        // Update English name fields
        if (request.FirstNameEn != null)
            user.FirstNameEn = string.IsNullOrWhiteSpace(request.FirstNameEn) ? null : request.FirstNameEn.Trim();
        if (request.MiddleNameEn != null)
            user.MiddleNameEn = string.IsNullOrWhiteSpace(request.MiddleNameEn) ? null : request.MiddleNameEn.Trim();
        if (request.LastNameEn != null)
            user.LastNameEn = string.IsNullOrWhiteSpace(request.LastNameEn) ? null : request.LastNameEn.Trim();

        if (request.MaritalStatusId.HasValue)
        {
            var maritalStatusExists = await _db.MaritalStatuses
                .AnyAsync(m => m.Id == request.MaritalStatusId.Value, ct);
            if (!maritalStatusExists)
                throw new InvalidOperationException("Invalid MaritalStatusId.");
            user.MaritalStatusId = request.MaritalStatusId;
        }

        if (request.AddressAr != null)
            user.AddressAr = string.IsNullOrWhiteSpace(request.AddressAr) ? null : request.AddressAr.Trim();

        if (request.AddressEn != null)
            user.AddressEn = string.IsNullOrWhiteSpace(request.AddressEn) ? null : request.AddressEn.Trim();

        if (request.GovernorateId.HasValue)
        {
            var governorateExists = await _db.Governorates
                .AnyAsync(g => g.Id == request.GovernorateId.Value, ct);
            if (!governorateExists)
                throw new InvalidOperationException("Invalid GovernorateId.");
            user.GovernorateId = request.GovernorateId;
        }

        if (request.CityId.HasValue)
        {
            var cityExists = await _db.Cities
                .AnyAsync(c => c.Id == request.CityId.Value, ct);
            if (!cityExists)
                throw new InvalidOperationException("Invalid CityId.");
            user.CityId = request.CityId;
        }
        
        if (request.Birthday.HasValue)
            user.Birthday = request.Birthday;

        await _db.SaveChangesAsync(ct);
        return user;
    }

    private AuthResponse CreateAuthResponse(AppUser user)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(_jwt.AccessTokenMinutes);

        var claims = new List<Claim>
{
    new(ClaimTypes.NameIdentifier, user.Id.ToString()), 
    new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
    new("nationalId", user.NationalId ?? user.PassportNumber ?? string.Empty),
    new("role", user.Role.ToString()),
    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
};

        if (!string.IsNullOrWhiteSpace(user.Email))
            claims.Add(new(JwtRegisteredClaimNames.Email, user.Email));

        // Add name claim from English or Arabic name
        var fullName = user.FirstNameEn ?? user.FirstNameAr;
        if (!string.IsNullOrWhiteSpace(fullName))
            claims.Add(new("name", fullName!));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: creds
        );

        return new AuthResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expires,
            Role = user.Role.ToString()
        };
    }

    private async Task SeedSuperAdminIfMissingAsync(IConfiguration configuration)
    {
        var nationalId = configuration["Auth:SuperAdmin:NationalId"]?.Trim();
        var email = configuration["Auth:SuperAdmin:Email"]?.Trim();
        var phoneNumber = configuration["Auth:SuperAdmin:PhoneNumber"]?.Trim();
        var password = configuration["Auth:SuperAdmin:Password"];
        var firstNameAr = configuration["Auth:SuperAdmin:FirstNameAr"]?.Trim();
        var middleNameAr = configuration["Auth:SuperAdmin:MiddleNameAr"]?.Trim();
        var lastNameAr = configuration["Auth:SuperAdmin:LastNameAr"]?.Trim();

        if (string.IsNullOrWhiteSpace(nationalId) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(phoneNumber))
            return;

        var exists = await _db.Users
            .AnyAsync(u => u.NationalId == nationalId 
                || (!string.IsNullOrWhiteSpace(email) && u.Email != null && u.Email.ToLower() == email.ToLower())
                || u.PhoneNumber == phoneNumber);
        if (exists)
            return;

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            NationalId = nationalId,
            Email = email,
            PhoneNumber = phoneNumber,
            PasswordHash = HashPassword(password),
            Role = AppRole.SuperAdmin,
            IsPending = false,
            FirstNameAr = firstNameAr,
            MiddleNameAr = middleNameAr,
            LastNameAr = lastNameAr
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    private async Task SeedHRIfMissingAsync(IConfiguration configuration)
    {
        var nationalId = configuration["Auth:HR:NationalId"]?.Trim();
        var email = configuration["Auth:HR:Email"]?.Trim();
        var phoneNumber = configuration["Auth:HR:PhoneNumber"]?.Trim();
        var password = configuration["Auth:HR:Password"];
        var firstNameAr = configuration["Auth:HR:FirstNameAr"]?.Trim();
        var middleNameAr = configuration["Auth:HR:MiddleNameAr"]?.Trim();
        var lastNameAr = configuration["Auth:HR:LastNameAr"]?.Trim();

        if (string.IsNullOrWhiteSpace(nationalId) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(phoneNumber))
            return;

        var exists = await _db.Users
            .AnyAsync(u => u.NationalId == nationalId 
                || (!string.IsNullOrWhiteSpace(email) && u.Email != null && u.Email.ToLower() == email.ToLower())
                || u.PhoneNumber == phoneNumber);
        if (exists)
            return;

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            NationalId = nationalId,
            Email = email,
            PhoneNumber = phoneNumber,
            PasswordHash = HashPassword(password),
            Role = AppRole.HR,
            IsPending = false,
            FirstNameAr = firstNameAr,
            MiddleNameAr = middleNameAr,
            LastNameAr = lastNameAr
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    // Format: v1.<iterations>.<saltBase64>.<subkeyBase64>
    private static string HashPassword(string password)
    {
        const int iterations = 100_000;
        var salt = RandomNumberGenerator.GetBytes(16);
        var subkey = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            32
        );

        return $"v1.{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(subkey)}";
    }

    private static bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 || !string.Equals(parts[0], "v1", StringComparison.Ordinal))
            return false;

        if (!int.TryParse(parts[1], out var iterations))
            return false;

        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expected.Length
        );

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    public async Task<bool> SendForgotPasswordOtpAsync(ForgotPasswordRequest request, CancellationToken ct)
    {
        var phoneNumber = request.PhoneNumber?.Trim();

        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, ct);

        if (user == null)
            return false;

        var otp = GenerateOtp();
        var (hash, salt) = HashOtp(otp);

        // Invalidate previous OTPs for this phone
        var existingOtps = await _db.PasswordResetOtps
            .Where(o => o.PhoneNumber == phoneNumber && !o.IsUsed && o.ExpiresAt > DateTime.Now)
            .ToListAsync(ct);
        if (existingOtps.Count > 0)
        {
            foreach (var item in existingOtps)
                item.IsUsed = true;
        }

        var otpRecord = new PasswordResetOtp
        {
            PhoneNumber = phoneNumber,
            OtpHash = hash,
            OtpSalt = salt,
            ExpiresAt = DateTime.Now.AddMinutes(3),
            IsUsed = false,
            FailedAttempts = 0
        };

        _db.PasswordResetOtps.Add(otpRecord);
        await _db.SaveChangesAsync(ct);

        try
        {
            var sent = await SendOtpSmsAsync(phoneNumber, otp, ct);
            if (!sent)
                throw new InvalidOperationException("Failed to send OTP.");
        }
        catch (InvalidOperationException)
        {
            otpRecord.IsUsed = true;
            otpRecord.UsedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);
            throw;
        }

        return true;
    }

    public async Task VerifyPasswordResetOtpAsync(VerifyResetOtpRequest request, CancellationToken ct)
    {
        var phoneNumber = request.PhoneNumber?.Trim();
        var otp = request.Otp?.Trim();

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new InvalidOperationException("PhoneNumber is required.");

        if (string.IsNullOrWhiteSpace(otp))
            throw new InvalidOperationException("Otp is required.");

        var otpRecord = await _db.PasswordResetOtps
            .Where(o => o.PhoneNumber == phoneNumber && !o.IsUsed && o.ExpiresAt > DateTime.Now)
            .OrderByDescending(o => o.Id)
            .FirstOrDefaultAsync(ct);

        if (otpRecord == null || !VerifyOtp(otp, otpRecord.OtpHash, otpRecord.OtpSalt))
        {
            if (otpRecord != null)
            {
                otpRecord.FailedAttempts += 1;
                await _db.SaveChangesAsync(ct);
            }
            throw new InvalidOperationException("Invalid or expired OTP.");
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        var phoneNumber = request.PhoneNumber?.Trim();
        var otp = request.Otp?.Trim();
        var newPassword = request.NewPassword;
        var confirmNewPassword = request.ConfirmNewPassword;

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new InvalidOperationException("PhoneNumber is required.");

        if (string.IsNullOrWhiteSpace(otp))
            throw new InvalidOperationException("Otp is required.");

        if (string.IsNullOrWhiteSpace(newPassword))
            throw new InvalidOperationException("New password is required.");

        if (string.IsNullOrWhiteSpace(confirmNewPassword))
            throw new InvalidOperationException("Confirm new password is required.");

        if (newPassword != confirmNewPassword)
            throw new InvalidOperationException("New password and confirm new password do not match.");

        var otpRecord = await _db.PasswordResetOtps
            .Where(o => o.PhoneNumber == phoneNumber && !o.IsUsed && o.ExpiresAt > DateTime.Now)
            .OrderByDescending(o => o.Id)
            .FirstOrDefaultAsync(ct);

        if (otpRecord == null || !VerifyOtp(otp, otpRecord.OtpHash, otpRecord.OtpSalt))
        {
            if (otpRecord != null)
            {
                otpRecord.FailedAttempts += 1;
                await _db.SaveChangesAsync(ct);
            }
            throw new InvalidOperationException("Invalid or expired OTP.");
        }

        // Get user and update password
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, ct);

        if (user == null)
            throw new InvalidOperationException("User not found.");

        // Hash and update password
        user.PasswordHash = HashPassword(newPassword);
        otpRecord.IsUsed = true;
        otpRecord.UsedAt = DateTime.Now;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<bool> SendOtpSmsAsync(string mobile, string otp, CancellationToken ct)
    {
        var template = string.IsNullOrWhiteSpace(_sms.OtpTemplate)
            ? "MCI OTP is {otp}, valid for 3 minutes"
            : _sms.OtpTemplate;

        var text = template.Replace("{otp}", otp, StringComparison.Ordinal);
        if (string.IsNullOrWhiteSpace(_sms.ApiToken))
            throw new InvalidOperationException("Sms:ApiToken is required.");

        const int maxRetries = 3;
        string? lastError = null;

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var result = await SmsHelper.SendSmsWithErrorAsync(
                    mobile,
                    text,
                    _sms.SenderId,
                    _sms.ApiToken,
                    _sms.ApiUrl,
                    ct);

                if (result.Success)
                    return true;

                if (!string.IsNullOrWhiteSpace(result.Error))
                    lastError = result.Error;
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"SMS request timed out. Attempt {attempt + 1} of {maxRetries}");
                lastError = "SMS request timed out.";
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"SMS request failed: {ex.Message}");
                lastError = $"SMS request failed: {ex.Message}";
            }

            await Task.Delay(1000, ct);
        }

        if (!string.IsNullOrWhiteSpace(lastError))
            throw new InvalidOperationException(lastError);

        return false;
    }

    private static string GenerateOtp()
    {
        Span<byte> bytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(bytes);
        var value = BitConverter.ToUInt32(bytes) % 1_000_000;
        return value.ToString("D6");
    }

    private static (string Hash, string Salt) HashOtp(string otp)
    {
        Span<byte> saltBytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(saltBytes);
        var salt = Convert.ToBase64String(saltBytes);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{otp}:{salt}"));
        return (Convert.ToBase64String(hash), salt);
    }

    private static bool VerifyOtp(string otp, string expectedHash, string salt)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{otp}:{salt}"));
        var actual = Convert.ToBase64String(hash);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(actual),
            Encoding.UTF8.GetBytes(expectedHash));
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            throw new InvalidOperationException("Current password is required.");

        if (string.IsNullOrWhiteSpace(request.NewPassword))
            throw new InvalidOperationException("New password is required.");

        if (string.IsNullOrWhiteSpace(request.ConfirmNewPassword))
            throw new InvalidOperationException("Confirm new password is required.");

        if (request.NewPassword != request.ConfirmNewPassword)
            throw new InvalidOperationException("New password and confirm new password do not match.");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
            throw new InvalidOperationException("User not found.");

        if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
            throw new InvalidOperationException("Current password is incorrect.");

        // Hash and update password
        user.PasswordHash = HashPassword(request.NewPassword);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<AppUser>> GetAllAdminsAsync(CancellationToken ct)
    {
        return await _db.Users
            .Where(u => u.Role == AppRole.Admin || u.Role == AppRole.SuperAdmin)
            .OrderBy(u => u.Role)
            .ThenBy(u => u.FirstNameEn ?? u.FirstNameAr ?? u.NationalId ?? u.PassportNumber)
            .ToListAsync(ct);
    }

    public async Task<List<AppUser>> GetAdminsByDepartmentIdAsync(int departmentId, CancellationToken ct)
    {
        return await _db.Users
            .Where(u => (u.Role == AppRole.Admin || u.Role == AppRole.SuperAdmin) 
                     && u.DepartmentId == departmentId)
            .OrderBy(u => u.Role)
            .ThenBy(u => u.FirstNameEn ?? u.FirstNameAr ?? u.NationalId ?? u.PassportNumber)
            .ToListAsync(ct);
    }

    public async Task<AppUser> CreateAdminAsync(CreateAdminRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var nationalId = request.NationalId?.Trim();
        var passportNumber = request.PassportNumber?.Trim();

        // Validate that at least one of NationalId or PassportNumber is provided
        if (string.IsNullOrWhiteSpace(nationalId) && string.IsNullOrWhiteSpace(passportNumber))
            throw new InvalidOperationException("Either NationalId or PassportNumber is required.");

        // Only SuperAdmin can create SuperAdmin
        if (request.Role == AppRole.SuperAdmin)
            throw new InvalidOperationException("Cannot create SuperAdmin through this endpoint. SuperAdmin must be configured in appsettings.json.");

        // Check uniqueness of NationalId if provided
        if (!string.IsNullOrWhiteSpace(nationalId))
        {
        var nationalIdExists = await _db.Users
            .AnyAsync(u => u.NationalId == nationalId, ct);
        if (nationalIdExists)
            throw new InvalidOperationException("NationalId already registered.");
        }

        // Check uniqueness of PassportNumber if provided
        if (!string.IsNullOrWhiteSpace(passportNumber))
        {
            var passportNumberExists = await _db.Users
                .AnyAsync(u => u.PassportNumber == passportNumber, ct);
            if (passportNumberExists)
                throw new InvalidOperationException("PassportNumber already registered.");
        }

        var phoneNumber = request.PhoneNumber.Trim();
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new InvalidOperationException("PhoneNumber is required.");

        var phoneNumberExists = await _db.Users
            .AnyAsync(u => u.PhoneNumber == phoneNumber, ct);
        if (phoneNumberExists)
            throw new InvalidOperationException("PhoneNumber already registered.");

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            NationalId = nationalId,
            PassportNumber = passportNumber,
            PasswordHash = HashPassword(request.Password),
            Role = request.Role,
            IsPending = false,
            Email = request.Email?.Trim(),
            FirstNameAr = request.FirstNameAr?.Trim(),
            MiddleNameAr = request.MiddleNameAr?.Trim(),
            LastNameAr = request.LastNameAr?.Trim(),
            FirstNameEn = request.FirstNameEn?.Trim(),
            MiddleNameEn = request.MiddleNameEn?.Trim(),
            LastNameEn = request.LastNameEn?.Trim(),
            PhoneNumber = phoneNumber,
            DepartmentId = request.DepartmentId
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return user;
    }

    public async Task UpdateAdminPasswordAsync(UpdateAdminPasswordRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var nationalId = request.NationalId.Trim();
        if (string.IsNullOrWhiteSpace(nationalId))
            throw new InvalidOperationException("NationalId is required.");

        if (string.IsNullOrWhiteSpace(request.NewPassword))
            throw new InvalidOperationException("New password is required.");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.NationalId == nationalId, ct);

        if (user == null)
            throw new InvalidOperationException("Admin not found.");

        // Verify it's an admin
        if (user.Role != AppRole.Admin && user.Role != AppRole.SuperAdmin)
            throw new InvalidOperationException("User is not an admin.");

        // Hash and update password
        user.PasswordHash = HashPassword(request.NewPassword);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<OrganizationChartResponse> GetOrganizationChartAsync(CancellationToken ct)
    {
        // Get all departments
        var departments = await _db.Departments
            .OrderBy(d => d.Name)
            .ToListAsync(ct);

        // Get all users (including admins and superadmins)
        var allUsers = await _db.Users
            .OrderBy(u => u.FirstNameEn ?? u.FirstNameAr ?? u.NationalId ?? u.PassportNumber)
            .ToListAsync(ct);

        // Find CEO (SuperAdmin) - if multiple, take the first one
        var ceo = allUsers.FirstOrDefault(u => u.Role == AppRole.SuperAdmin);
        
        if (ceo == null)
        {
            throw new InvalidOperationException("No SuperAdmin (CEO) found in the system.");
        }

        // Build CEO user
        var ceoUser = new OrganizationChartUser
        {
            Id = ceo.Id,
            FirstNameAr = ceo.FirstNameAr,
            MiddleNameAr = ceo.MiddleNameAr,
            LastNameAr = ceo.LastNameAr,
            FirstNameEn = ceo.FirstNameEn,
            MiddleNameEn = ceo.MiddleNameEn,
            LastNameEn = ceo.LastNameEn,
            ImageUrl = ceo.ImageUrl,
            Level = "CEO"
        };

        // Build department groups with their managers and employees
        var departmentGroups = new List<DepartmentGroup>();

        foreach (var department in departments)
        {
            // Find all Admins (Managers) for this department
            var managers = allUsers
                .Where(u => u.DepartmentId == department.Id && u.Role == AppRole.Admin)
                .Select(u => new OrganizationChartUser
                {
                    Id = u.Id,
                    FirstNameAr = u.FirstNameAr,
                    MiddleNameAr = u.MiddleNameAr,
                    LastNameAr = u.LastNameAr,
                    FirstNameEn = u.FirstNameEn,
                    MiddleNameEn = u.MiddleNameEn,
                    LastNameEn = u.LastNameEn,
                    ImageUrl = u.ImageUrl,
                    Level = "MANAGER"
                })
                .ToList();

            // Get all Employees (Users) in this department
            var employees = allUsers
                .Where(u => u.DepartmentId == department.Id && u.Role == AppRole.User)
                .Select(u => new OrganizationChartUser
                {
                    Id = u.Id,
                    FirstNameAr = u.FirstNameAr,
                    MiddleNameAr = u.MiddleNameAr,
                    LastNameAr = u.LastNameAr,
                    FirstNameEn = u.FirstNameEn,
                    MiddleNameEn = u.MiddleNameEn,
                    LastNameEn = u.LastNameEn,
                    ImageUrl = u.ImageUrl,
                    Level = "EMPLOYEE"
                })
                .ToList();

            // If there are managers for this department, add the department group
            if (managers.Count > 0)
            {
                departmentGroups.Add(new DepartmentGroup
                {
                    Id = department.Id,
                    Name = department.Name,
                    Manager = managers.FirstOrDefault(),
                    Managers = managers,
                    Employees = employees
                });
            }
        }

        return new OrganizationChartResponse
        {
            CEO = ceoUser,
            Departments = departmentGroups
        };
    }


    public async Task UpdateUserRoleAsync(Guid userId, UpdateUserRoleRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
            throw new InvalidOperationException("User not found.");

        if (!Enum.TryParse<AppRole>(request.Role, out var role))
            throw new InvalidOperationException($"Invalid role: {request.Role}");

        if (role == AppRole.SuperAdmin)
            throw new InvalidOperationException("Cannot change user role to SuperAdmin through this endpoint. SuperAdmin must be configured in appsettings.json.");

        user.Role = role;
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateUserRoleByHRAsync(Guid userId, UpdateUserRoleRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
            throw new InvalidOperationException("User not found.");

        // HR cannot change role for SuperAdmin users
        if (user.Role == AppRole.SuperAdmin)
            throw new InvalidOperationException("Cannot change role for SuperAdmin users.");

        if (!Enum.TryParse<AppRole>(request.Role, out var role))
            throw new InvalidOperationException($"Invalid role: {request.Role}");

        // HR cannot change role to SuperAdmin
        if (role == AppRole.SuperAdmin)
            throw new InvalidOperationException("HR cannot change user role to SuperAdmin.");

        user.Role = role;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AuthResponse> AddEmployeeAsync(AddEmployeeForm request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var nationalId = request.NationalId?.Trim();
        var passportNumber = request.PassportNumber?.Trim();

        // Validate that at least one of NationalId or PassportNumber is provided
        if (string.IsNullOrWhiteSpace(nationalId) && string.IsNullOrWhiteSpace(passportNumber))
            throw new InvalidOperationException("Either NationalId or PassportNumber is required.");

        var phoneNumber = request.PhoneNumber.Trim();
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new InvalidOperationException("PhoneNumber is required.");

        // Check uniqueness of NationalId if provided
        if (!string.IsNullOrWhiteSpace(nationalId))
        {
            var nationalIdExists = await _db.Users
                .AnyAsync(u => u.NationalId == nationalId, ct);
            if (nationalIdExists)
                throw new InvalidOperationException("NationalId already registered.");
        }

        // Check uniqueness of PassportNumber if provided
        if (!string.IsNullOrWhiteSpace(passportNumber))
        {
            var passportNumberExists = await _db.Users
                .AnyAsync(u => u.PassportNumber == passportNumber, ct);
            if (passportNumberExists)
                throw new InvalidOperationException("PassportNumber already registered.");
        }

        var phoneNumberExists = await _db.Users
            .AnyAsync(u => u.PhoneNumber == phoneNumber, ct);
        if (phoneNumberExists)
            throw new InvalidOperationException("PhoneNumber already registered.");

        // Validate EmployeeCode if provided
        if (!string.IsNullOrWhiteSpace(request.EmployeeCode))
        {
            var employeeCodeExists = await _db.Users
                .AnyAsync(u => u.EmployeeCode == request.EmployeeCode, ct);
            if (employeeCodeExists)
                throw new InvalidOperationException("EmployeeCode already exists.");
        }

        // Validate Foreign Keys
        if (request.NationalityId.HasValue)
        {
            var nationalityExists = await _db.Nationalities
                .AnyAsync(n => n.Id == request.NationalityId.Value, ct);
            if (!nationalityExists)
                throw new InvalidOperationException("Invalid NationalityId.");
        }

        if (request.BranchId.HasValue)
        {
            var branchExists = await _db.Branches
                .AnyAsync(b => b.Id == request.BranchId.Value, ct);
            if (!branchExists)
                throw new InvalidOperationException("Invalid BranchId.");
        }

        if (request.JobId.HasValue)
        {
            var jobExists = await _db.JobTitles
                .AnyAsync(j => j.Id == request.JobId.Value, ct);
            if (!jobExists)
                throw new InvalidOperationException("Invalid JobId.");
        }

        if (!string.IsNullOrWhiteSpace(request.ManagerId) && Guid.TryParse(request.ManagerId, out var managerGuid))
        {
            var managerExists = await _db.Users
                .AnyAsync(u => u.Id == managerGuid, ct);
            if (!managerExists)
                throw new InvalidOperationException("Invalid ManagerId.");
        }

        if (request.MaritalStatusId.HasValue)
        {
            var maritalStatusExists = await _db.MaritalStatuses
                .AnyAsync(m => m.Id == request.MaritalStatusId.Value, ct);
            if (!maritalStatusExists)
                throw new InvalidOperationException("Invalid MaritalStatusId.");
        }

        if (request.EmploymentModeId.HasValue)
        {
            var employmentModeExists = await _db.EmploymentModes
                .AnyAsync(e => e.Id == request.EmploymentModeId.Value, ct);
            if (!employmentModeExists)
                throw new InvalidOperationException("Invalid EmploymentModeId.");
        }

        if (request.GovernorateId.HasValue)
        {
            var governorateExists = await _db.Governorates
                .AnyAsync(g => g.Id == request.GovernorateId.Value, ct);
            if (!governorateExists)
                throw new InvalidOperationException("Invalid GovernorateId.");
        }

        if (request.CityId.HasValue)
        {
            var cityExists = await _db.Cities
                .AnyAsync(c => c.Id == request.CityId.Value, ct);
            if (!cityExists)
                throw new InvalidOperationException("Invalid CityId.");
        }

        if (request.InsuranceCompanyId.HasValue)
        {
            var insuranceCompanyExists = await _db.InsuranceCompanies
                .AnyAsync(ic => ic.Id == request.InsuranceCompanyId.Value, ct);
            if (!insuranceCompanyExists)
                throw new InvalidOperationException("Invalid InsuranceCompanyId.");
        }

        // Generate EmployeeCode if not provided
        var employeeCode = request.EmployeeCode?.Trim();
        if (string.IsNullOrWhiteSpace(employeeCode))
        {
            // Generate a unique employee code
            var lastEmployee = await _db.Users
                .Where(u => !string.IsNullOrEmpty(u.EmployeeCode))
                .OrderByDescending(u => u.EmployeeCode)
                .FirstOrDefaultAsync(ct);
            
            if (lastEmployee != null && !string.IsNullOrEmpty(lastEmployee.EmployeeCode))
            {
                if (int.TryParse(lastEmployee.EmployeeCode, out var lastCode))
                    employeeCode = (lastCode + 1).ToString();
                else
                    employeeCode = $"EMP{DateTime.Now:yyyyMMddHHmmss}";
            }
            else
            {
                employeeCode = "EMP00001";
            }
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            NationalId = nationalId,
            PassportNumber = passportNumber,
            EmployeeCode = employeeCode,
            PasswordHash = HashPassword(request.Password),
            Role = AppRole.User,
            IsMale = request.IsMale,
            IsPending = false,
            Email = request.Email?.Trim(),
            FirstNameAr = request.FirstNameAr?.Trim(),
            MiddleNameAr = request.MiddleNameAr?.Trim(),
            LastNameAr = request.LastNameAr?.Trim(),
            FirstNameEn = request.FirstNameEn?.Trim(),
            MiddleNameEn = request.MiddleNameEn?.Trim(),
            LastNameEn = request.LastNameEn?.Trim(),
            MachineCode = request.MachineCode?.Trim(),
            FingerprintKey = request.FingerprintKey?.Trim(),
            AllowMobileAttendanceFromAnyLocation = request.AllowMobileAttendanceFromAnyLocation,
            NationalityId = request.NationalityId,
            Religion = request.Religion,
            BranchId = request.BranchId,
            JobId = request.JobId,
            ManagerId = !string.IsNullOrWhiteSpace(request.ManagerId) && Guid.TryParse(request.ManagerId, out var mgrGuid) ? mgrGuid : null,
            MaritalStatusId = request.MaritalStatusId,
            AddressAr = request.AddressAr?.Trim(),
            AddressEn = request.AddressEn?.Trim(),
            EmploymentModeId = request.EmploymentModeId,
            GovernorateId = request.GovernorateId,
            CityId = request.CityId,
            IsActive = request.IsActive,
            IsDisabled = request.IsDisabled,
            SickLeaveBalance = request.SickLeaveBalance,
            PhoneNumber = phoneNumber,
            DepartmentId = request.DepartmentId,
            StartDate = request.StartDate,
            ContractEndDate = request.ContractEndDate,
            GrossSalary = request.GrossSalary,
            ShiftRate = request.ShiftRate,
            OvertimeRate = request.OvertimeRate,
            WorkEarningsTax = request.WorkEarningsTax,
            InsuranceSalary = request.InsuranceSalary,
            IsInsured = request.IsInsured,
            InsuranceCompanyId = request.InsuranceCompanyId,
            HousingAllowance = request.HousingAllowance,
            MealAllowance = request.MealAllowance,
            TransportationAllowance = request.TransportationAllowance,
            InsuranceAllowance = request.InsuranceAllowance,
            CompanyPhoneNumber = request.CompanyPhoneNumber?.Trim(),
            CompanyEmail = request.CompanyEmail?.Trim(),
            Birthday = request.Birthday,
            WorkType = request.WorkType
        };

        if (request.WorkType == WorkType.Hybrid && request.WorkFromHomeDays != null && request.WorkFromHomeDays.Any())
        {
            user.WorkFromHomeDays = JsonSerializer.Serialize(request.WorkFromHomeDays);
        }

        // Work schedule (per-employee) based on EmploymentModeId
        await ApplyWorkScheduleForNewEmployeeAsync(user, request, ct);

        // Bank info (1:1)
        var hasAnyBankInfo =
            !string.IsNullOrWhiteSpace(request.BankName)
            || !string.IsNullOrWhiteSpace(request.AccountNumber)
            || !string.IsNullOrWhiteSpace(request.IbanNumber)
            || !string.IsNullOrWhiteSpace(request.SwiftBicCode)
            || !string.IsNullOrWhiteSpace(request.BankBranchCode);

        if (hasAnyBankInfo)
        {
            user.BankInfo = new internalEmployee.Data.Entities.EmployeeBankInfo
            {
                UserId = user.Id,
                BankName = request.BankName?.Trim(),
                AccountNumber = request.AccountNumber?.Trim(),
                Iban = request.IbanNumber?.Trim(),
                SwiftBic = request.SwiftBicCode?.Trim(),
                BranchCode = request.BankBranchCode?.Trim()
            };
        }

        // Education (1:N)
        if (request.Educations is { Count: > 0 })
        {
            foreach (var e in request.Educations)
            {
                // Skip completely empty entries
                var isEmpty =
                    string.IsNullOrWhiteSpace(e.UniversityName)
                    && !e.GraduationYear.HasValue
                    && string.IsNullOrWhiteSpace(e.Degree)
                    && string.IsNullOrWhiteSpace(e.FinalGrade);
                if (isEmpty) continue;

                user.Educations.Add(new internalEmployee.Data.Entities.EmployeeEducation
                {
                    UserId = user.Id,
                    UniversityName = e.UniversityName?.Trim(),
                    GraduationYear = e.GraduationYear,
                    Degree = e.Degree?.Trim(),
                    FinalGrade = e.FinalGrade?.Trim()
                });
            }
        }

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        // Assign default company location to newly added employee if configured
        if (_companyLocation.Latitude != 0 || _companyLocation.Longitude != 0)
        {
            var userLocation = new internalEmployee.Data.Entities.UserLocation
            {
                UserId = user.Id,
                Name = _companyLocation.Name,
                Latitude = _companyLocation.Latitude,
                Longitude = _companyLocation.Longitude,
                RadiusMeters = _companyLocation.RadiusMeters,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _db.UserLocations.Add(userLocation);
            await _db.SaveChangesAsync(ct);
        }

        // Upload profile image if provided
        if (request.Image != null && request.Image.Length > 0)
        {
            user.ImageUrl = await SaveEmployeeImageAsync(user.Id, request.Image, null, ct);
            await _db.SaveChangesAsync(ct);
        }

        // Upload attachments if provided
        if (request.Attachments != null && request.Attachments.Length > 0)
        {
            var files = request.Attachments.Where(f => f != null && f.Length > 0).ToList();
            if (files.Count > 0)
            {
                await _attachmentService.UploadAttachmentsAsync(user.Id, files, ct);
            }
        }

        return CreateAuthResponse(user);
    }

    private async Task ApplyWorkScheduleForNewEmployeeAsync(AppUser user, AddEmployeeForm request, CancellationToken ct)
    {
        var modeName = await GetEmploymentModeNameAsync(request.EmploymentModeId, ct);
        var isPartTime = string.Equals(modeName, "Part Time", StringComparison.OrdinalIgnoreCase);
        var isShift = string.Equals(modeName, "Shift", StringComparison.OrdinalIgnoreCase);
        var partTimeUseDefaultWeek = request.PartTimeUseDefaultWeek;

        if (isPartTime)
        {
            if (!request.PartTimeStart.HasValue || !request.PartTimeEnd.HasValue)
                throw new InvalidOperationException("PartTimeStart and PartTimeEnd are required when EmploymentMode is Part Time.");

            if (!partTimeUseDefaultWeek)
            {
                var days = NormalizePartTimeWorkDays(request.PartTimeWorkDays ?? new List<DayOfWeek>());
                if (days.Count == 0)
                    throw new InvalidOperationException("PartTimeWorkDays is required when PartTimeUseDefaultWeek is false.");
            }
        }

        if (isShift)
        {
            if (!request.ShiftRate.HasValue || request.ShiftRate.Value <= 0)
                throw new InvalidOperationException("ShiftRate is required and must be greater than 0 when EmploymentMode is Shift.");
        }

        user.WorkSchedule = new EmployeeWorkSchedule
        {
            UserId = user.Id,
            PartTimeStart = request.PartTimeStart,
            PartTimeEnd = request.PartTimeEnd,
            PartTimeUseDefaultWeek = isPartTime ? partTimeUseDefaultWeek : true,
            PartTimeCustomDaysJson = isPartTime && !partTimeUseDefaultWeek
                ? SerializePartTimeWorkDays(request.PartTimeWorkDays)
                : null
        };

        // Weekly shifts removed for Shift mode
    }

    private async Task<string?> GetEmploymentModeNameAsync(int? employmentModeId, CancellationToken ct)
    {
        if (!employmentModeId.HasValue)
            return null;

        var mode = await _db.EmploymentModes
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == employmentModeId.Value, ct);
        return mode?.Name;
    }

    public async Task<EmployeeDetailsResponse> UpdateEmployeeAsync(Guid employeeId, UpdateEmployeeForm request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var user = await _db.Users
            .Include(u => u.BankInfo)
            .Include(u => u.Educations)
            .Include(u => u.WorkSchedule)
            .FirstOrDefaultAsync(u => u.Id == employeeId, ct);

        if (user == null)
            throw new InvalidOperationException("Employee not found.");

        // Update basic fields (only if provided)
        if (request.NationalId != null)
        {
            var nationalId = request.NationalId.Trim();
            if (!string.IsNullOrWhiteSpace(nationalId))
            {
                var exists = await _db.Users
                    .AnyAsync(u => u.NationalId == nationalId && u.Id != employeeId, ct);
                if (exists)
                    throw new InvalidOperationException("NationalId already registered.");
                user.NationalId = nationalId;
            }
        }

        if (request.PassportNumber != null)
        {
            var passportNumber = request.PassportNumber.Trim();
            if (!string.IsNullOrWhiteSpace(passportNumber))
            {
                var exists = await _db.Users
                    .AnyAsync(u => u.PassportNumber == passportNumber && u.Id != employeeId, ct);
                if (exists)
                    throw new InvalidOperationException("PassportNumber already registered.");
                user.PassportNumber = passportNumber;
            }
        }

        if (request.PhoneNumber != null)
        {
            var phoneNumber = request.PhoneNumber.Trim();
            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                var exists = await _db.Users
                    .AnyAsync(u => u.PhoneNumber == phoneNumber && u.Id != employeeId, ct);
                if (exists)
                    throw new InvalidOperationException("PhoneNumber already registered.");
                user.PhoneNumber = phoneNumber;
            }
        }

        if (request.EmployeeCode != null)
        {
            var employeeCode = request.EmployeeCode.Trim();
            if (!string.IsNullOrWhiteSpace(employeeCode))
            {
                var exists = await _db.Users
                    .AnyAsync(u => u.EmployeeCode == employeeCode && u.Id != employeeId, ct);
                if (exists)
                    throw new InvalidOperationException("EmployeeCode already exists.");
                user.EmployeeCode = employeeCode;
            }
        }

        // Update name fields (handle empty strings as null)
        if (request.FirstNameAr != null) user.FirstNameAr = string.IsNullOrWhiteSpace(request.FirstNameAr) ? null : request.FirstNameAr.Trim();
        if (request.MiddleNameAr != null) user.MiddleNameAr = string.IsNullOrWhiteSpace(request.MiddleNameAr) ? null : request.MiddleNameAr.Trim();
        if (request.LastNameAr != null) user.LastNameAr = string.IsNullOrWhiteSpace(request.LastNameAr) ? null : request.LastNameAr.Trim();
        if (request.FirstNameEn != null) user.FirstNameEn = string.IsNullOrWhiteSpace(request.FirstNameEn) ? null : request.FirstNameEn.Trim();
        if (request.MiddleNameEn != null) user.MiddleNameEn = string.IsNullOrWhiteSpace(request.MiddleNameEn) ? null : request.MiddleNameEn.Trim();
        if (request.LastNameEn != null) user.LastNameEn = string.IsNullOrWhiteSpace(request.LastNameEn) ? null : request.LastNameEn.Trim();

        // Update basic fields
        if (request.IsMale.HasValue) user.IsMale = request.IsMale.Value;
        if (request.Religion.HasValue) user.Religion = request.Religion.Value;
        if (request.Birthday.HasValue) user.Birthday = request.Birthday;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
        if (request.IsDisabled.HasValue) user.IsDisabled = request.IsDisabled.Value;
        if (request.AllowMobileAttendanceFromAnyLocation.HasValue) user.AllowMobileAttendanceFromAnyLocation = request.AllowMobileAttendanceFromAnyLocation.Value;
        if (request.SickLeaveBalance.HasValue) user.SickLeaveBalance = request.SickLeaveBalance.Value;

        // Update contact fields (handle empty strings as null)
        if (request.Email != null) user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        if (request.AddressAr != null) user.AddressAr = string.IsNullOrWhiteSpace(request.AddressAr) ? null : request.AddressAr.Trim();
        if (request.AddressEn != null) user.AddressEn = string.IsNullOrWhiteSpace(request.AddressEn) ? null : request.AddressEn.Trim();

        // Update work fields
        if (request.StartDate.HasValue) user.StartDate = request.StartDate;
        if (request.ContractEndDate.HasValue) user.ContractEndDate = request.ContractEndDate;
        if (request.GrossSalary.HasValue) user.GrossSalary = request.GrossSalary;
        if (request.ShiftRate.HasValue) user.ShiftRate = request.ShiftRate;
        if (request.OvertimeRate.HasValue) user.OvertimeRate = request.OvertimeRate;
        if (request.WorkEarningsTax.HasValue) user.WorkEarningsTax = request.WorkEarningsTax;
        if (request.InsuranceSalary.HasValue) user.InsuranceSalary = request.InsuranceSalary;
        if (request.IsInsured.HasValue) user.IsInsured = request.IsInsured.Value;
        if (request.HousingAllowance.HasValue) user.HousingAllowance = request.HousingAllowance;
        if (request.MealAllowance.HasValue) user.MealAllowance = request.MealAllowance;
        if (request.TransportationAllowance.HasValue) user.TransportationAllowance = request.TransportationAllowance;
        if (request.InsuranceAllowance.HasValue) user.InsuranceAllowance = request.InsuranceAllowance;
        if (request.CompanyEmail != null) user.CompanyEmail = request.CompanyEmail.Trim();
        if (request.CompanyPhoneNumber != null) user.CompanyPhoneNumber = request.CompanyPhoneNumber.Trim();

        if (request.WorkType.HasValue) user.WorkType = request.WorkType.Value;
        if (request.WorkType == WorkType.Hybrid)
        {
            if (request.WorkFromHomeDays != null && request.WorkFromHomeDays.Any())
            {
                user.WorkFromHomeDays = JsonSerializer.Serialize(request.WorkFromHomeDays);
            }
        }
        else if (request.WorkType.HasValue)
        {
             user.WorkFromHomeDays = null;
        }

        // Update additional fields (handle empty strings as null)
        if (request.MachineCode != null) user.MachineCode = string.IsNullOrWhiteSpace(request.MachineCode) ? null : request.MachineCode.Trim();
        if (request.FingerprintKey != null) user.FingerprintKey = string.IsNullOrWhiteSpace(request.FingerprintKey) ? null : request.FingerprintKey.Trim();

        // Validate and update Foreign Keys
        if (request.NationalityId.HasValue)
        {
            var exists = await _db.Nationalities.AnyAsync(n => n.Id == request.NationalityId.Value, ct);
            if (!exists) throw new InvalidOperationException("Invalid NationalityId.");
            user.NationalityId = request.NationalityId;
        }

        if (request.BranchId.HasValue)
        {
            var exists = await _db.Branches.AnyAsync(b => b.Id == request.BranchId.Value, ct);
            if (!exists) throw new InvalidOperationException("Invalid BranchId.");
            user.BranchId = request.BranchId;
        }

        if (request.JobId.HasValue)
        {
            var exists = await _db.JobTitles.AnyAsync(j => j.Id == request.JobId.Value, ct);
            if (!exists) throw new InvalidOperationException("Invalid JobId.");
            user.JobId = request.JobId;
        }

        if (request.ManagerId != null)
        {
            if (string.IsNullOrWhiteSpace(request.ManagerId))
            {
                user.ManagerId = null;
            }
            else if (Guid.TryParse(request.ManagerId, out var managerGuid))
            {
                var exists = await _db.Users.AnyAsync(u => u.Id == managerGuid, ct);
                if (!exists) throw new InvalidOperationException("Invalid ManagerId.");
                user.ManagerId = managerGuid;
            }
        }

        if (request.MaritalStatusId.HasValue)
        {
            var exists = await _db.MaritalStatuses.AnyAsync(m => m.Id == request.MaritalStatusId.Value, ct);
            if (!exists) throw new InvalidOperationException("Invalid MaritalStatusId.");
            user.MaritalStatusId = request.MaritalStatusId;
        }

        if (request.EmploymentModeId.HasValue)
        {
            var exists = await _db.EmploymentModes.AnyAsync(e => e.Id == request.EmploymentModeId.Value, ct);
            if (!exists) throw new InvalidOperationException("Invalid EmploymentModeId.");
            user.EmploymentModeId = request.EmploymentModeId;
        }

        if (request.GovernorateId.HasValue)
        {
            var exists = await _db.Governorates.AnyAsync(g => g.Id == request.GovernorateId.Value, ct);
            if (!exists) throw new InvalidOperationException("Invalid GovernorateId.");
            user.GovernorateId = request.GovernorateId;
        }

        if (request.CityId.HasValue)
        {
            var exists = await _db.Cities.AnyAsync(c => c.Id == request.CityId.Value, ct);
            if (!exists) throw new InvalidOperationException("Invalid CityId.");
            user.CityId = request.CityId;
        }

        if (request.DepartmentId.HasValue)
        {
            var exists = await _db.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value, ct);
            if (!exists) throw new InvalidOperationException("Invalid DepartmentId.");
            user.DepartmentId = request.DepartmentId;
        }

        if (request.InsuranceCompanyId.HasValue)
        {
            var exists = await _db.InsuranceCompanies.AnyAsync(ic => ic.Id == request.InsuranceCompanyId.Value, ct);
            if (!exists) throw new InvalidOperationException("Invalid InsuranceCompanyId.");
            user.InsuranceCompanyId = request.InsuranceCompanyId;
        }

        // Update WorkSchedule (per-employee) based on EmploymentModeId
        var scheduleTouched =
            request.EmploymentModeId.HasValue
            || request.PartTimeStart.HasValue
            || request.PartTimeEnd.HasValue
            || request.PartTimeUseDefaultWeek.HasValue
            || request.PartTimeWorkDays != null;

        if (scheduleTouched)
        {
            var modeName = await GetEmploymentModeNameAsync(request.EmploymentModeId ?? user.EmploymentModeId, ct);
            var isPartTime = string.Equals(modeName, "Part Time", StringComparison.OrdinalIgnoreCase);
            var isShift = string.Equals(modeName, "Shift", StringComparison.OrdinalIgnoreCase);

            user.WorkSchedule ??= new EmployeeWorkSchedule { UserId = user.Id };

            if (isPartTime)
            {
                // If switching to part-time, require both times (either already stored or provided now)
                var start = request.PartTimeStart ?? user.WorkSchedule.PartTimeStart;
                var end = request.PartTimeEnd ?? user.WorkSchedule.PartTimeEnd;
                if (!start.HasValue || !end.HasValue)
                    throw new InvalidOperationException("PartTimeStart and PartTimeEnd are required when EmploymentMode is Part Time.");

                var useDefaultWeek = request.PartTimeUseDefaultWeek ?? user.WorkSchedule.PartTimeUseDefaultWeek;
                user.WorkSchedule.PartTimeUseDefaultWeek = useDefaultWeek;

                if (useDefaultWeek)
                {
                    user.WorkSchedule.PartTimeCustomDaysJson = null;
                }
                else
                {
                    var selectedDays = request.PartTimeWorkDays ?? DeserializePartTimeWorkDays(user.WorkSchedule.PartTimeCustomDaysJson);
                    var normalizedDays = NormalizePartTimeWorkDays(selectedDays ?? new List<DayOfWeek>());
                    if (normalizedDays.Count == 0)
                        throw new InvalidOperationException("PartTimeWorkDays is required when PartTimeUseDefaultWeek is false.");

                    user.WorkSchedule.PartTimeCustomDaysJson = SerializePartTimeWorkDays(normalizedDays);
                }

                user.WorkSchedule.PartTimeStart = start;
                user.WorkSchedule.PartTimeEnd = end;
            }
            else
            {
                // Keep provided time overrides if sent. Update part-time schedule if provided instead of resetting.
                if (request.PartTimeStart.HasValue) user.WorkSchedule.PartTimeStart = request.PartTimeStart;
                if (request.PartTimeEnd.HasValue) user.WorkSchedule.PartTimeEnd = request.PartTimeEnd;
                
                if (request.PartTimeUseDefaultWeek.HasValue)
                {
                    user.WorkSchedule.PartTimeUseDefaultWeek = request.PartTimeUseDefaultWeek.Value;
                    
                    if (request.PartTimeUseDefaultWeek.Value)
                    {
                        user.WorkSchedule.PartTimeCustomDaysJson = null;
                    }
                    else if (request.PartTimeWorkDays != null)
                    {
                        var normalizedDays = NormalizePartTimeWorkDays(request.PartTimeWorkDays);
                        if (normalizedDays.Count > 0)
                            user.WorkSchedule.PartTimeCustomDaysJson = SerializePartTimeWorkDays(normalizedDays);
                    }
                }
            }

            if (isShift)
            {
                var effectiveShiftRate = request.ShiftRate ?? user.ShiftRate;
                if (!effectiveShiftRate.HasValue || effectiveShiftRate.Value <= 0)
                    throw new InvalidOperationException("ShiftRate is required and must be greater than 0 when EmploymentMode is Shift.");
            }
        }

        // Update BankInfo (handle empty strings as null)
        var hasAnyBankInfo = !string.IsNullOrWhiteSpace(request.BankName)
            || !string.IsNullOrWhiteSpace(request.AccountNumber)
            || !string.IsNullOrWhiteSpace(request.IbanNumber)
            || !string.IsNullOrWhiteSpace(request.SwiftBicCode)
            || !string.IsNullOrWhiteSpace(request.BankBranchCode);

        if (hasAnyBankInfo)
        {
            if (user.BankInfo == null)
            {
                user.BankInfo = new internalEmployee.Data.Entities.EmployeeBankInfo
                {
                    UserId = user.Id
                };
            }

            if (request.BankName != null) user.BankInfo.BankName = string.IsNullOrWhiteSpace(request.BankName) ? null : request.BankName.Trim();
            if (request.AccountNumber != null) user.BankInfo.AccountNumber = string.IsNullOrWhiteSpace(request.AccountNumber) ? null : request.AccountNumber.Trim();
            if (request.IbanNumber != null) user.BankInfo.Iban = string.IsNullOrWhiteSpace(request.IbanNumber) ? null : request.IbanNumber.Trim();
            if (request.SwiftBicCode != null) user.BankInfo.SwiftBic = string.IsNullOrWhiteSpace(request.SwiftBicCode) ? null : request.SwiftBicCode.Trim();
            if (request.BankBranchCode != null) user.BankInfo.BranchCode = string.IsNullOrWhiteSpace(request.BankBranchCode) ? null : request.BankBranchCode.Trim();
        }

        // Update Educations (handle empty strings as null)
        if (request.Educations != null && request.Educations.Count > 0)
        {
            foreach (var edu in request.Educations)
            {
                // Skip completely empty entries
                var isEmpty = string.IsNullOrWhiteSpace(edu.UniversityName)
                    && !edu.GraduationYear.HasValue
                    && string.IsNullOrWhiteSpace(edu.Degree)
                    && string.IsNullOrWhiteSpace(edu.FinalGrade);
                if (isEmpty) continue;

                if (edu.Id.HasValue)
                {
                    // Update existing education
                    var existingEdu = user.Educations.FirstOrDefault(e => e.Id == edu.Id.Value);
                    if (existingEdu != null)
                    {
                        if (edu.UniversityName != null) existingEdu.UniversityName = string.IsNullOrWhiteSpace(edu.UniversityName) ? null : edu.UniversityName.Trim();
                        if (edu.GraduationYear.HasValue) existingEdu.GraduationYear = edu.GraduationYear;
                        if (edu.Degree != null) existingEdu.Degree = string.IsNullOrWhiteSpace(edu.Degree) ? null : edu.Degree.Trim();
                        if (edu.FinalGrade != null) existingEdu.FinalGrade = string.IsNullOrWhiteSpace(edu.FinalGrade) ? null : edu.FinalGrade.Trim();
                    }
                }
                else
                {
                    // Add new education - add directly to DbSet to ensure it's tracked properly
                    var newEducation = new internalEmployee.Data.Entities.EmployeeEducation
                    {
                        UserId = user.Id,
                        UniversityName = string.IsNullOrWhiteSpace(edu.UniversityName) ? null : edu.UniversityName?.Trim(),
                        GraduationYear = edu.GraduationYear,
                        Degree = string.IsNullOrWhiteSpace(edu.Degree) ? null : edu.Degree?.Trim(),
                        FinalGrade = string.IsNullOrWhiteSpace(edu.FinalGrade) ? null : edu.FinalGrade?.Trim()
                    };
                    _db.EmployeeEducations.Add(newEducation);
                    // Also add to collection to keep it in sync
                    user.Educations.Add(newEducation);
                }
            }
        }

        // Save all changes
        var savedChanges = await _db.SaveChangesAsync(ct);
        
        // Detach and reload the entity to ensure changes are persisted and we get fresh data
        _db.Entry(user).State = EntityState.Detached;
        
        // Reload the entity with all related data
        user = await _db.Users
            .Include(u => u.BankInfo)
            .Include(u => u.Educations)
            .Include(u => u.WorkSchedule)
            .FirstOrDefaultAsync(u => u.Id == employeeId, ct);
        
        if (user == null)
            throw new InvalidOperationException("Employee not found after update.");

        // Upload new attachments if provided
        if (request.Attachments != null && request.Attachments.Length > 0)
        {
            var files = request.Attachments.Where(f => f != null && f.Length > 0).ToList();
            if (files.Count > 0)
            {
                await _attachmentService.UploadAttachmentsAsync(user.Id, files, ct);
            }
        }

        // Upload profile image if provided
        if (request.Image != null && request.Image.Length > 0)
        {
            user.ImageUrl = await SaveEmployeeImageAsync(user.Id, request.Image, user.ImageUrl, ct);
            await _db.SaveChangesAsync(ct);
            // Reload after image update
            await _db.Entry(user).ReloadAsync(ct);
        }

        // Return updated employee details - use fresh query to ensure we get latest data
        return await GetEmployeeByIdWithDetailsAsync(employeeId, ct)
            ?? throw new InvalidOperationException("Failed to retrieve updated employee details.");
    }

    private static List<DayOfWeek> NormalizePartTimeWorkDays(IEnumerable<DayOfWeek> days)
    {
        return days
            .Distinct()
            .OrderBy(d => (int)d)
            .ToList();
    }

    private static string? SerializePartTimeWorkDays(IEnumerable<DayOfWeek>? days)
    {
        if (days == null)
            return null;

        var normalized = NormalizePartTimeWorkDays(days);
        return normalized.Count == 0 ? null : JsonSerializer.Serialize(normalized);
    }

    private static List<DayOfWeek>? DeserializePartTimeWorkDays(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var values = JsonSerializer.Deserialize<List<DayOfWeek>>(json);
            return values == null ? null : NormalizePartTimeWorkDays(values);
        }
        catch
        {
            return null;
        }
    }

    private static readonly string[] EmployeeImageAllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };

    private async Task<string> SaveEmployeeImageAsync(Guid userId, IFormFile image, string? existingRelativeUrl, CancellationToken ct)
    {
        if (image == null || image.Length == 0)
            throw new InvalidOperationException("Image file is empty.");

        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        if (!EmployeeImageAllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Invalid image format. Allowed formats: jpg, jpeg, png, gif, bmp");

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{userId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        // Save new file
        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await image.CopyToAsync(stream, ct);
        }

        // Delete old image if exists
        if (!string.IsNullOrWhiteSpace(existingRelativeUrl))
        {
            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingRelativeUrl.TrimStart('/'));
            if (File.Exists(oldPath))
            {
                try
                {
                    File.Delete(oldPath);
                }
                catch
                {
                    // ignore deletion errors
                }
            }
        }

        return $"/uploads/profiles/{fileName}";
    }

    public async Task ChangeEmployeeActiveStatusAsync(Guid employeeId, bool isActive, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == employeeId, ct);

        if (user == null)
            throw new InvalidOperationException("Employee not found.");

        // Prevent deactivating SuperAdmin
        if (user.Role == AppRole.SuperAdmin && !isActive)
            throw new InvalidOperationException("Cannot deactivate SuperAdmin user.");

        user.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteEmployeeAsync(Guid employeeId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var user = await _db.Users
            .Include(u => u.BankInfo)
            .Include(u => u.Educations)
            .FirstOrDefaultAsync(u => u.Id == employeeId, ct);

        if (user == null)
            throw new InvalidOperationException("Employee not found.");

        // Prevent deleting SuperAdmin
        if (user.Role == AppRole.SuperAdmin)
            throw new InvalidOperationException("Cannot delete SuperAdmin user.");

        // Delete related data
        if (user.BankInfo != null)
        {
            _db.Set<internalEmployee.Data.Entities.EmployeeBankInfo>().Remove(user.BankInfo);
        }

        if (user.Educations != null && user.Educations.Count > 0)
        {
            _db.Set<internalEmployee.Data.Entities.EmployeeEducation>().RemoveRange(user.Educations);
        }

        // Delete user attachments (files will be handled by UserAttachmentService if needed)
        var attachments = await _db.UserAttachments
            .Where(a => a.UserId == employeeId)
            .ToListAsync(ct);
        if (attachments.Count > 0)
        {
            _db.UserAttachments.RemoveRange(attachments);
        }

        // Delete the user
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteEmployeeImageAsync(Guid employeeId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == employeeId, ct);

        if (user == null)
            throw new InvalidOperationException("Employee not found.");

        if (string.IsNullOrWhiteSpace(user.ImageUrl))
            return;

        var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ImageUrl.TrimStart('/'));
        if (File.Exists(imagePath))
        {
            try
            {
                File.Delete(imagePath);
            }
            catch
            {
                // ignore delete errors
            }
        }

        user.ImageUrl = null;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ChangeSalaryAsync(Guid employeeId, ChangeSalaryRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == employeeId, ct);
        if (user == null)
            throw new InvalidOperationException("Employee not found.");

        if (user.GrossSalary == request.NewSalary)
            return;

        // Update salary
        user.GrossSalary = request.NewSalary;
        
        // Save changes. AppDbContext's SaveChangesAsync override will handle the automatic history tracking.
        // If a reason is provided, we could potentially set it in the DbContext if we had a way to pass it to the override,
        // but for now, the override uses "تحديث تلقائي للراتب".
        // To support custom reason, we would need to manually create the history or use EmployeeHistoryService.
        
        await _db.SaveChangesAsync(ct);
    }

    public async Task TransferDepartmentAsync(Guid employeeId, TransferDepartmentRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == employeeId, ct);
        if (user == null)
            throw new InvalidOperationException("Employee not found.");

        if (user.DepartmentId == request.NewDepartmentId)
            return;

        // Verify department exists
        var departmentExists = await _db.Departments.AnyAsync(d => d.Id == request.NewDepartmentId, ct);
        if (!departmentExists)
            throw new InvalidOperationException("Department not found.");

        // Update department
        user.DepartmentId = request.NewDepartmentId;

        // Save changes. AppDbContext's SaveChangesAsync override will handle the automatic history tracking.
        await _db.SaveChangesAsync(ct);
    }

    public async Task ChangeJobTitleAsync(Guid employeeId, ChangeJobTitleRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == employeeId, ct);
        if (user == null)
            throw new InvalidOperationException("Employee not found.");

        if (user.JobId == request.NewJobId)
            return;

        // Verify job title exists
        var jobExists = await _db.JobTitles.AnyAsync(j => j.Id == request.NewJobId, ct);
        if (!jobExists)
            throw new InvalidOperationException("Job title not found.");

        // Update job ID
        user.JobId = request.NewJobId;

        // Save changes. AppDbContext's SaveChangesAsync override will handle the automatic history tracking.
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAllowancesAsync(Guid employeeId, UpdateAllowancesRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == employeeId, ct);
        if (user == null)
            throw new InvalidOperationException("Employee not found.");

        user.HousingAllowance = request.HousingAllowance;
        user.MealAllowance = request.MealAllowance;
        user.TransportationAllowance = request.TransportationAllowance;
        user.InsuranceAllowance = request.InsuranceAllowance;

        await _db.SaveChangesAsync(ct);
    }
    private string GetFullName(AppUser user)
    {
        var arName = $"{user.FirstNameAr} {user.MiddleNameAr} {user.LastNameAr}".Replace("  ", " ").Trim();
        var enName = $"{user.FirstNameEn} {user.MiddleNameEn} {user.LastNameEn}".Replace("  ", " ").Trim();

        return !string.IsNullOrWhiteSpace(arName) ? arName : enName;
    }

    private async Task<string?> GetDepartmentManagerNameAsync(int? departmentId, CancellationToken ct)
    {
        if (!departmentId.HasValue) return null;

        var manager = await _db.Users
            .AsNoTracking()
            .Where(u => u.DepartmentId == departmentId.Value && u.Role == AppRole.Admin)
            .FirstOrDefaultAsync(ct);

        return manager != null ? GetFullName(manager) : null;
    }
}
