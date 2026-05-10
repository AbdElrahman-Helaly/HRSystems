using internalEmployee.Auth.Contracts;
using internalEmployee.Data;
using internalEmployee.Services.Auth;
using internalEmployee.Services.Attachment;
using internalEmployee.Services.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using OfficeOpenXml;
using OfficeOpenXml.DataValidation;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IUserAttachmentService _attachmentService;

    public AuthController(IAuthService authService, AppDbContext db, INotificationService notificationService, IUserAttachmentService attachmentService)
    {
        _authService = authService;
        _db = db;
        _notificationService = notificationService;
        _attachmentService = attachmentService;
    }

   

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _authService.LoginAsync(request, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        try
        {
            var sent = await _authService.SendForgotPasswordOtpAsync(request, ct);
            if (!sent)
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "PhoneNumber not found.");

            return Ok(new { message = "OTP sent successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPost("verify-reset-otp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> VerifyResetOtp([FromBody] VerifyResetOtpRequest request, CancellationToken ct)
    {
        try
        {
            await _authService.VerifyPasswordResetOtpAsync(request, ct);
            return Ok(new { message = "OTP verified successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        try
        {
            await _authService.ResetPasswordAsync(request, ct);
            return Ok(new { message = "Password has been reset successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpGet("users")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(PaginatedResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResponse<UserResponse>>> GetAllUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] int? departmentId = null,
        [FromQuery] int? branchId = null,
        [FromQuery] int? jobId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
    {
        var responses = await _authService.GetAllUsersWithDetailsAsync(
            pageNumber, pageSize, search, departmentId, branchId, jobId, isActive, ct);
        
        // Add absolute image urls — reconstruct PaginatedResponse since Items is init-only
        var pagedResult = new PaginatedResponse<UserResponse>
        {
            PageNumber = responses.PageNumber,
            PageSize = responses.PageSize,
            TotalCount = responses.TotalCount,
            Items = responses.Items.Select(r => new UserResponse
            {
                Id = r.Id,
                NationalId = r.NationalId,
                PassportNumber = r.PassportNumber,
                Email = r.Email,
                FirstNameAr = r.FirstNameAr,
                MiddleNameAr = r.MiddleNameAr,
                LastNameAr = r.LastNameAr,
                FirstNameEn = r.FirstNameEn,
                MiddleNameEn = r.MiddleNameEn,
                LastNameEn = r.LastNameEn,
                MachineCode = r.MachineCode,
                FingerprintKey = r.FingerprintKey,
                AllowMobileAttendanceFromAnyLocation = r.AllowMobileAttendanceFromAnyLocation,
                NationalityId = r.NationalityId,
                NationalityName = r.NationalityName,
                Religion = r.Religion,
                EmployeeCode = r.EmployeeCode,
                BranchId = r.BranchId,
                BranchName = r.BranchName,
                JobId = r.JobId,
                JobTitleName = r.JobTitleName,
                ManagerId = r.ManagerId,
                ManagerName = r.ManagerName,
                DepartmentManagerName = r.DepartmentManagerName,
                MaritalStatusId = r.MaritalStatusId,
                MaritalStatusName = r.MaritalStatusName,
                AddressAr = r.AddressAr,
                AddressEn = r.AddressEn,
                EmploymentModeId = r.EmploymentModeId,
                EmploymentModeName = r.EmploymentModeName,
                GovernorateId = r.GovernorateId,
                GovernorateName = r.GovernorateName,
                CityId = r.CityId,
                CityName = r.CityName,
                IsActive = r.IsActive,
                PhoneNumber = r.PhoneNumber,
                DepartmentId = r.DepartmentId,
                DepartmentName = r.DepartmentName,
                StartDate = r.StartDate,
                CompanyPhoneNumber = r.CompanyPhoneNumber,
                CompanyEmail = r.CompanyEmail,
                ImageUrl = GetImageUrl(r.ImageUrl),
                Role = r.Role,
                IsMale = r.IsMale,
                IsPending = r.IsPending,
                Birthday = r.Birthday
            }).ToList()
        };
        return Ok(pagedResult);
    }

    [HttpGet("employees")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(List<EmployeeDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<EmployeeDetailsResponse>>> GetAllEmployees(CancellationToken ct)
    {
        var employees = await _authService.GetAllEmployeesWithDetailsAsync(ct);

        // Make attachment urls absolute + image url absolute
        var responses = employees.Select(e =>
        {
            var mappedAttachments = e.Attachments.Select(a =>
            {
                a.FileUrl = GetImageUrl(a.FilePath);
                return a;
            }).ToList();

            return new EmployeeDetailsResponse
            {
                Id = e.Id,
                NationalId = e.NationalId,
                PassportNumber = e.PassportNumber,
                Email = e.Email,
                FirstNameAr = e.FirstNameAr,
                MiddleNameAr = e.MiddleNameAr,
                LastNameAr = e.LastNameAr,
                FirstNameEn = e.FirstNameEn,
                MiddleNameEn = e.MiddleNameEn,
                LastNameEn = e.LastNameEn,
                MachineCode = e.MachineCode,
                FingerprintKey = e.FingerprintKey,
                AllowMobileAttendanceFromAnyLocation = e.AllowMobileAttendanceFromAnyLocation,
                NationalityId = e.NationalityId,
                NationalityName = e.NationalityName,
                Religion = e.Religion,
                EmployeeCode = e.EmployeeCode,
                BranchId = e.BranchId,
                BranchName = e.BranchName,
                JobId = e.JobId,
                JobTitleName = e.JobTitleName,
                ManagerId = e.ManagerId,
                ManagerName = e.ManagerName,
                MaritalStatusId = e.MaritalStatusId,
                MaritalStatusName = e.MaritalStatusName,
                AddressAr = e.AddressAr,
                AddressEn = e.AddressEn,
                EmploymentModeId = e.EmploymentModeId,
                EmploymentModeName = e.EmploymentModeName,
                GovernorateId = e.GovernorateId,
                GovernorateName = e.GovernorateName,
                CityId = e.CityId,
                CityName = e.CityName,
                IsActive = e.IsActive,
                IsDisabled = e.IsDisabled,
                SickLeaveBalance = e.SickLeaveBalance,
                PhoneNumber = e.PhoneNumber,
                DepartmentId = e.DepartmentId,
                DepartmentName = e.DepartmentName,
                StartDate = e.StartDate,
                ContractEndDate = e.ContractEndDate,
                GrossSalary = e.GrossSalary,
                ShiftRate = e.ShiftRate,
                HousingAllowance = e.HousingAllowance,
                MealAllowance = e.MealAllowance,
                TransportationAllowance = e.TransportationAllowance,
                InsuranceAllowance = e.InsuranceAllowance,
                OvertimeRate = e.OvertimeRate,
                WorkEarningsTax = e.WorkEarningsTax,
                InsuranceSalary = e.InsuranceSalary,
                IsInsured = e.IsInsured,
                InsuranceCompanyId = e.InsuranceCompanyId,
                InsuranceCompanyName = e.InsuranceCompanyName,
                PartTimeStart = e.PartTimeStart,
                PartTimeEnd = e.PartTimeEnd,
                PartTimeUseDefaultWeek = e.PartTimeUseDefaultWeek,
                PartTimeWorkDays = e.PartTimeWorkDays,
                CompanyPhoneNumber = e.CompanyPhoneNumber,
                CompanyEmail = e.CompanyEmail,
                ImageUrl = GetImageUrl(e.ImageUrl),
                Role = e.Role,
                IsMale = e.IsMale,
                IsPending = e.IsPending,
                Birthday = e.Birthday,
                WorkType = e.WorkType,
                WorkFromHomeDays = e.WorkFromHomeDays,
                BankInfo = e.BankInfo,
                Educations = e.Educations,
                Attachments = mappedAttachments
            };
        }).ToList();

        return Ok(responses);
    }



    [HttpGet("employee/{id}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(EmployeeDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeDetailsResponse>> GetEmployeeById(Guid id, CancellationToken ct)
    {
        var response = await _authService.GetEmployeeByIdWithDetailsAsync(id, ct);
        if (response == null)
            return NotFound();

        // Convert image url and attachment urls to absolute
        response = new EmployeeDetailsResponse
        {
            // base fields
            Id = response.Id,
            NationalId = response.NationalId,
            PassportNumber = response.PassportNumber,
            Email = response.Email,
            FirstNameAr = response.FirstNameAr,
            MiddleNameAr = response.MiddleNameAr,
            LastNameAr = response.LastNameAr,
            FirstNameEn = response.FirstNameEn,
            MiddleNameEn = response.MiddleNameEn,
            LastNameEn = response.LastNameEn,
            MachineCode = response.MachineCode,
            FingerprintKey = response.FingerprintKey,
            AllowMobileAttendanceFromAnyLocation = response.AllowMobileAttendanceFromAnyLocation,
            NationalityId = response.NationalityId,
            NationalityName = response.NationalityName,
            Religion = response.Religion,
            EmployeeCode = response.EmployeeCode,
            BranchId = response.BranchId,
            BranchName = response.BranchName,
            JobId = response.JobId,
            JobTitleName = response.JobTitleName,
            ManagerId = response.ManagerId,
            ManagerName = response.ManagerName,
            DepartmentManagerName = response.DepartmentManagerName,
            MaritalStatusId = response.MaritalStatusId,
            MaritalStatusName = response.MaritalStatusName,
            AddressAr = response.AddressAr,
            AddressEn = response.AddressEn,
            EmploymentModeId = response.EmploymentModeId,
            EmploymentModeName = response.EmploymentModeName,
            GovernorateId = response.GovernorateId,
            GovernorateName = response.GovernorateName,
            CityId = response.CityId,
            CityName = response.CityName,
            IsActive = response.IsActive,
            IsDisabled = response.IsDisabled,
            SickLeaveBalance = response.SickLeaveBalance,
            PhoneNumber = response.PhoneNumber,
            DepartmentId = response.DepartmentId,
            DepartmentName = response.DepartmentName,
            StartDate = response.StartDate,
            ContractEndDate = response.ContractEndDate,
            GrossSalary = response.GrossSalary,
            ShiftRate = response.ShiftRate,
            HousingAllowance = response.HousingAllowance,
            MealAllowance = response.MealAllowance,
            TransportationAllowance = response.TransportationAllowance,
            InsuranceAllowance = response.InsuranceAllowance,
            OvertimeRate = response.OvertimeRate,
            WorkEarningsTax = response.WorkEarningsTax,
            InsuranceSalary = response.InsuranceSalary,
            IsInsured = response.IsInsured,
            InsuranceCompanyId = response.InsuranceCompanyId,
            InsuranceCompanyName = response.InsuranceCompanyName,
            PartTimeStart = response.PartTimeStart,
            PartTimeEnd = response.PartTimeEnd,
            PartTimeUseDefaultWeek = response.PartTimeUseDefaultWeek,
            PartTimeWorkDays = response.PartTimeWorkDays,
            CompanyPhoneNumber = response.CompanyPhoneNumber,
            CompanyEmail = response.CompanyEmail,
            ImageUrl = GetImageUrl(response.ImageUrl),
            Role = response.Role,
            IsMale = response.IsMale,
            IsPending = response.IsPending,
            Birthday = response.Birthday,
            WorkType = response.WorkType,
            WorkFromHomeDays = response.WorkFromHomeDays,
            BankInfo = response.BankInfo,
            Educations = response.Educations,
            Attachments = response.Attachments
                .Select(a => new EmployeeAttachmentResponse
                {
                    Id = a.Id,
                    OriginalFileName = a.OriginalFileName,
                    ContentType = a.ContentType,
                    FileSize = a.FileSize,
                    UploadedAt = a.UploadedAt,
                    FilePath = a.FilePath,
                    FileUrl = GetImageUrl(a.FilePath)
                })
                .ToList()
        };

        return Ok(response);
    }

    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> GetMyProfile(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Console.WriteLine(userIdClaim);
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var response = await _authService.GetUserProfileWithDetailsAsync(userId, ct);
        if (response == null)
            return NotFound();

        // Apply GetImageUrl transformation (GetImageUrl needs HttpContext which is only available in Controller)
        var user = await _authService.GetUserByIdAsync(userId, ct);
        if (user != null && response != null)
        {
            // Create new response with updated ImageUrl since UserProfileResponse is not a record type
            response = new UserProfileResponse
            {
                Id = response.Id,
                NationalId = response.NationalId,
                PassportNumber = response.PassportNumber,
                Email = response.Email,
                FirstNameAr = response.FirstNameAr,
                MiddleNameAr = response.MiddleNameAr,
                LastNameAr = response.LastNameAr,
                FirstNameEn = response.FirstNameEn,
                MiddleNameEn = response.MiddleNameEn,
                LastNameEn = response.LastNameEn,
                MachineCode = response.MachineCode,
                FingerprintKey = response.FingerprintKey,
                AllowMobileAttendanceFromAnyLocation = response.AllowMobileAttendanceFromAnyLocation,
                NationalityId = response.NationalityId,
                NationalityName = response.NationalityName,
                Religion = response.Religion,
                EmployeeCode = response.EmployeeCode,
                BranchId = response.BranchId,
                BranchName = response.BranchName,
                JobId = response.JobId,
                JobTitleName = response.JobTitleName,
                ManagerId = response.ManagerId,
                ManagerName = response.ManagerName,
                DepartmentManagerName = response.DepartmentManagerName,
                MaritalStatusId = response.MaritalStatusId,
                MaritalStatusName = response.MaritalStatusName,
                AddressAr = response.AddressAr,
                AddressEn = response.AddressEn,
                EmploymentModeId = response.EmploymentModeId,
                EmploymentModeName = response.EmploymentModeName,
                GovernorateId = response.GovernorateId,
                GovernorateName = response.GovernorateName,
                CityId = response.CityId,
                CityName = response.CityName,
                IsActive = response.IsActive,
                PhoneNumber = response.PhoneNumber,
                DepartmentId = response.DepartmentId,
                DepartmentName = response.DepartmentName,
                StartDate = response.StartDate,
                CompanyPhoneNumber = response.CompanyPhoneNumber,
                CompanyEmail = response.CompanyEmail,
            ImageUrl = GetImageUrl(user.ImageUrl),
                Role = response.Role,
                IsMale = response.IsMale,
                IsPending = response.IsPending,
                Birthday = response.Birthday,
                WorkType = response.WorkType,
                WorkFromHomeDays = response.WorkFromHomeDays
            };
        }

        return Ok(response);
    }

    [HttpPut("profile")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserProfileResponse>> UpdateMyProfile(
        [FromForm] UpdateProfileForm form,
        CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var user = await _authService.GetUserByIdAsync(userId, ct);
            if (user == null)
                return NotFound();

            // Handle image upload if provided
            if (form.Image != null && form.Image.Length > 0)
            {
                // Validate image file
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                var fileExtension = Path.GetExtension(form.Image.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                    return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid image format. Allowed formats: jpg, jpeg, png, gif, bmp");

                // Create uploads directory if it doesn't exist
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Generate unique filename
                var fileName = $"{userId}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await form.Image.CopyToAsync(stream, ct);
                }

                // Delete old image if exists
                if (!string.IsNullOrWhiteSpace(user.ImageUrl))
                {
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                        catch
                        {
                            // Ignore deletion errors
                        }
                    }
                }

                // Store relative URL
                user.ImageUrl = $"/uploads/profiles/{fileName}";
            }

            // Create UpdateProfileRequest from form fields
            DateOnly? parsedBirthday = null;
            if (!string.IsNullOrWhiteSpace(form.Birthday) && DateOnly.TryParse(form.Birthday, out var birthdayDate))
            {
                parsedBirthday = birthdayDate;
            }

            int? parsedMaritalStatusId = null;
            if (!string.IsNullOrWhiteSpace(form.MaritalStatusId?.ToString()) && int.TryParse(form.MaritalStatusId.ToString(), out var maritalStatusId))
            {
                parsedMaritalStatusId = maritalStatusId;
            }

            int? parsedGovernorateId = null;
            if (!string.IsNullOrWhiteSpace(form.GovernorateId?.ToString()) && int.TryParse(form.GovernorateId.ToString(), out var governorateId))
            {
                parsedGovernorateId = governorateId;
            }

            int? parsedCityId = null;
            if (!string.IsNullOrWhiteSpace(form.CityId?.ToString()) && int.TryParse(form.CityId.ToString(), out var cityId))
            {
                parsedCityId = cityId;
            }

            var updateRequest = new UpdateProfileRequest
            {
                Email = form.Email,
                FirstNameAr = form.FirstNameAr,
                MiddleNameAr = form.MiddleNameAr,
                LastNameAr = form.LastNameAr,
                FirstNameEn = form.FirstNameEn,
                MiddleNameEn = form.MiddleNameEn,
                LastNameEn = form.LastNameEn,
                MaritalStatusId = parsedMaritalStatusId,
                AddressAr = form.AddressAr,
                AddressEn = form.AddressEn,
                GovernorateId = parsedGovernorateId,
                CityId = parsedCityId,
                Birthday = parsedBirthday
            };

            // Update profile data
            user = await _authService.UpdateUserProfileAsync(userId, updateRequest, ct);

            // Get department name
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
            string? departmentManagerName = null;
            if (user.DepartmentId.HasValue)
            {
                var dManager = await _db.Users.FirstOrDefaultAsync(u => u.DepartmentId == user.DepartmentId && u.Role == internalEmployee.Auth.Models.AppRole.Admin, ct);
                if (dManager != null)
                {
                    var arName = $"{dManager.FirstNameAr} {dManager.MiddleNameAr} {dManager.LastNameAr}".Replace("  ", " ").Trim();
                    var enName = $"{dManager.FirstNameEn} {dManager.MiddleNameEn} {dManager.LastNameEn}".Replace("  ", " ").Trim();
                    departmentManagerName = !string.IsNullOrWhiteSpace(arName) ? arName : enName;
                }
            }

            if (user.ManagerId.HasValue)
            {
                var manager = await _db.Users
                    .FirstOrDefaultAsync(u => u.Id == user.ManagerId.Value, ct);
                if (manager != null)
                {
                    var arName = $"{manager.FirstNameAr} {manager.MiddleNameAr} {manager.LastNameAr}".Replace("  ", " ").Trim();
                    var enName = $"{manager.FirstNameEn} {manager.MiddleNameEn} {manager.LastNameEn}".Replace("  ", " ").Trim();
                    managerName = !string.IsNullOrWhiteSpace(arName) ? arName : enName;
                }
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

            var response = new UserProfileResponse
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
                PhoneNumber = user.PhoneNumber,
                DepartmentId = user.DepartmentId,
                DepartmentName = departmentName,
                JobTitle = user.JobTitle,
                StartDate = user.StartDate,
                CompanyPhoneNumber = user.CompanyPhoneNumber,
                CompanyEmail = user.CompanyEmail,
                ImageUrl = GetImageUrl(user.ImageUrl),
                Role = user.Role.ToString(),
                IsMale = user.IsMale,
                IsPending = user.IsPending,
                Birthday = user.Birthday
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: ex.Message);
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            await _authService.ChangePasswordAsync(userId, request, ct);
            return Ok(new { message = "Password has been changed successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    private string? GetImageUrl(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl))
            return null;

        // Get base URL from request
        var request = HttpContext.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}";
        
        // Ensure relativeUrl starts with /
        if (!relativeUrl.StartsWith("/"))
            relativeUrl = "/" + relativeUrl;

        return baseUrl + relativeUrl;
    }

   

    [HttpGet("admins/department/{departmentId}")]
    [Authorize(Roles = "SuperAdmin,HR,Admin")]
    [ProducesResponseType(typeof(List<AdminInfoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<AdminInfoResponse>>> GetAdminsByDepartmentId(int departmentId, CancellationToken ct)
    {
        try
        {
            // Validate department exists
            var departmentExists = await _db.Departments.AnyAsync(d => d.Id == departmentId, ct);
            if (!departmentExists)
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Department not found.");
            
            var admins = await _authService.GetAdminsByDepartmentIdAsync(departmentId, ct);
            var responses = new List<AdminInfoResponse>();
            
            foreach (var admin in admins)
            {
            string? departmentName = null;
            if (admin.DepartmentId.HasValue)
            {
                var department = await _db.Departments
                    .FirstOrDefaultAsync(d => d.Id == admin.DepartmentId.Value, ct);
                departmentName = department?.Name;
            }

                responses.Add(new AdminInfoResponse
            {
                Id = admin.Id,
                NationalId = admin.NationalId,
                    FirstNameAr = admin.FirstNameAr,
                    MiddleNameAr = admin.MiddleNameAr,
                    LastNameAr = admin.LastNameAr,
                    FirstNameEn = admin.FirstNameEn,
                    MiddleNameEn = admin.MiddleNameEn,
                    LastNameEn = admin.LastNameEn,
                Email = admin.Email,
                Role = admin.Role.ToString(),
                DepartmentId = admin.DepartmentId,
                DepartmentName = departmentName
                });
            }

            return Ok(responses);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: ex.Message);
        }
    }




    [HttpGet("organization-chart")]
    [Authorize(Roles = "SuperAdmin,Admin,HR,User")]
    [ProducesResponseType(typeof(OrganizationChartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OrganizationChartResponse>> GetOrganizationChart(CancellationToken ct)
    {
        try
        {
            var result = await _authService.GetOrganizationChartAsync(ct);
            
            // Convert image URLs to full URLs
            var response = new OrganizationChartResponse
            {
                CEO = new OrganizationChartUser
                {
                    Id = result.CEO.Id,
                    FirstNameAr = result.CEO.FirstNameAr,
                    MiddleNameAr = result.CEO.MiddleNameAr,
                    LastNameAr = result.CEO.LastNameAr,
                    FirstNameEn = result.CEO.FirstNameEn,
                    MiddleNameEn = result.CEO.MiddleNameEn,
                    LastNameEn = result.CEO.LastNameEn,
                    ImageUrl = GetImageUrl(result.CEO.ImageUrl),
                    Level = result.CEO.Level
                },
                Departments = result.Departments.Select(d => new DepartmentGroup
                {
                    Id = d.Id,
                    Name = d.Name,
                    Manager = d.Manager == null ? null : new OrganizationChartUser
                    {
                        Id = d.Manager.Id,
                        FirstNameAr = d.Manager.FirstNameAr,
                        MiddleNameAr = d.Manager.MiddleNameAr,
                        LastNameAr = d.Manager.LastNameAr,
                        FirstNameEn = d.Manager.FirstNameEn,
                        MiddleNameEn = d.Manager.MiddleNameEn,
                        LastNameEn = d.Manager.LastNameEn,
                        ImageUrl = GetImageUrl(d.Manager.ImageUrl),
                        Level = d.Manager.Level
                    },
                    Managers = d.Managers.Select(m => new OrganizationChartUser
                    {
                        Id = m.Id,
                        FirstNameAr = m.FirstNameAr,
                        MiddleNameAr = m.MiddleNameAr,
                        LastNameAr = m.LastNameAr,
                        FirstNameEn = m.FirstNameEn,
                        MiddleNameEn = m.MiddleNameEn,
                        LastNameEn = m.LastNameEn,
                        ImageUrl = GetImageUrl(m.ImageUrl),
                        Level = m.Level
                    }).ToList(),
                    Employees = d.Employees.Select(e => new OrganizationChartUser
                    {
                        Id = e.Id,
                        FirstNameAr = e.FirstNameAr,
                        MiddleNameAr = e.MiddleNameAr,
                        LastNameAr = e.LastNameAr,
                        FirstNameEn = e.FirstNameEn,
                        MiddleNameEn = e.MiddleNameEn,
                        LastNameEn = e.LastNameEn,
                        ImageUrl = GetImageUrl(e.ImageUrl),
                        Level = e.Level
                    }).ToList()
                }).ToList()
            };
            
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }


    [HttpPut("users/{id}/role")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateUserRole(Guid id, [FromBody] UpdateUserRoleRequest request, CancellationToken ct)
    {
        try
        {
            await _authService.UpdateUserRoleAsync(id, request, ct);
            return Ok(new { message = "User role has been updated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPut("hr/users/{id}/role")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateUserRoleByHR(Guid id, [FromBody] UpdateUserRoleRequest request, CancellationToken ct)
    {
        try
        {
            await _authService.UpdateUserRoleByHRAsync(id, request, ct);
            return Ok(new { message = "User role has been updated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPost("fcm-token")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RegisterFcmToken([FromBody] RegisterFcmTokenRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            await _notificationService.RegisterFcmTokenAsync(userId, request.Token, request.DeviceInfo, ct);
            return Ok(new { message = "FCM token registered successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpDelete("fcm-token/{token}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> UnregisterFcmToken(string token, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        await _notificationService.UnregisterFcmTokenAsync(userId, token, ct);
        return Ok(new { message = "FCM token unregistered successfully." });
    }

    [HttpPut("employee/{id}")]
    [Authorize(Roles = "HR,Admin,SuperAdmin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(EmployeeDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeDetailsResponse>> UpdateEmployee(Guid id, [FromForm] UpdateEmployeeForm request, CancellationToken ct)
    {
        try
        {
            // Parse Educations from form data (they can come as separate form fields)
            // Check if Educations are sent as separate fields (for single education entry)
            if (Request.Form.ContainsKey("universityName") || 
                Request.Form.ContainsKey("graduationYear") || 
                Request.Form.ContainsKey("degree") || 
                Request.Form.ContainsKey("finalGrade"))
            {
                var universityName = Request.Form["universityName"].FirstOrDefault()?.ToString();
                var graduationYearStr = Request.Form["graduationYear"].FirstOrDefault()?.ToString();
                var degree = Request.Form["degree"].FirstOrDefault()?.ToString();
                var finalGrade = Request.Form["finalGrade"].FirstOrDefault()?.ToString();
                
                // Only create education if at least one field is provided
                if (!string.IsNullOrWhiteSpace(universityName) ||
                    !string.IsNullOrWhiteSpace(graduationYearStr) ||
                    !string.IsNullOrWhiteSpace(degree) ||
                    !string.IsNullOrWhiteSpace(finalGrade))
                {
                    DateOnly? graduationYear = null;
                    if (!string.IsNullOrWhiteSpace(graduationYearStr) && 
                        DateOnly.TryParse(graduationYearStr, out var parsedDate))
                    {
                        graduationYear = parsedDate;
                    }
                    
                    // Initialize Educations list if null
                    if (request.Educations == null)
                        request.Educations = new List<UpdateEmployeeEducationForm>();
                    
                    // Add the education entry (no Id means it's a new entry)
                    request.Educations.Add(new UpdateEmployeeEducationForm
                    {
                        Id = null, // New education entry
                        UniversityName = string.IsNullOrWhiteSpace(universityName) ? null : universityName,
                        GraduationYear = graduationYear,
                        Degree = string.IsNullOrWhiteSpace(degree) ? null : degree,
                        FinalGrade = string.IsNullOrWhiteSpace(finalGrade) ? null : finalGrade
                    });
                }
            }
            
            var result = await _authService.UpdateEmployeeAsync(id, request, ct);
            
            // Apply GetImageUrl to attachments
            var user = await _authService.GetUserByIdAsync(id, ct);
            if (user != null)
            {
                result = new EmployeeDetailsResponse
                {
                    Id = result.Id,
                    NationalId = result.NationalId,
                    PassportNumber = result.PassportNumber,
                    Email = result.Email,
                    FirstNameAr = result.FirstNameAr,
                    MiddleNameAr = result.MiddleNameAr,
                    LastNameAr = result.LastNameAr,
                    FirstNameEn = result.FirstNameEn,
                    MiddleNameEn = result.MiddleNameEn,
                    LastNameEn = result.LastNameEn,
                    MachineCode = result.MachineCode,
                    FingerprintKey = result.FingerprintKey,
                    AllowMobileAttendanceFromAnyLocation = result.AllowMobileAttendanceFromAnyLocation,
                    NationalityId = result.NationalityId,
                    NationalityName = result.NationalityName,
                    Religion = result.Religion,
                    EmployeeCode = result.EmployeeCode,
                    BranchId = result.BranchId,
                    BranchName = result.BranchName,
                    JobId = result.JobId,
                    JobTitleName = result.JobTitleName,
                    ManagerId = result.ManagerId,
                    ManagerName = result.ManagerName,
                    MaritalStatusId = result.MaritalStatusId,
                    MaritalStatusName = result.MaritalStatusName,
                    AddressAr = result.AddressAr,
                    AddressEn = result.AddressEn,
                    EmploymentModeId = result.EmploymentModeId,
                    EmploymentModeName = result.EmploymentModeName,
                    GovernorateId = result.GovernorateId,
                    GovernorateName = result.GovernorateName,
                    CityId = result.CityId,
                    CityName = result.CityName,
                    IsActive = result.IsActive,
                    PhoneNumber = result.PhoneNumber,
                    DepartmentId = result.DepartmentId,
                    DepartmentName = result.DepartmentName,
                    StartDate = result.StartDate,
                    ContractEndDate = result.ContractEndDate,
                    GrossSalary = result.GrossSalary,
                    ShiftRate = result.ShiftRate,
                    HousingAllowance = result.HousingAllowance,
                    MealAllowance = result.MealAllowance,
                    TransportationAllowance = result.TransportationAllowance,
                    InsuranceAllowance = result.InsuranceAllowance,
                    OvertimeRate = result.OvertimeRate,
                    InsuranceSalary = result.InsuranceSalary,
                    IsInsured = result.IsInsured,
                    InsuranceCompanyId = result.InsuranceCompanyId,
                    InsuranceCompanyName = result.InsuranceCompanyName,
                    PartTimeStart = result.PartTimeStart,
                    PartTimeEnd = result.PartTimeEnd,
                    PartTimeUseDefaultWeek = result.PartTimeUseDefaultWeek,
                    PartTimeWorkDays = result.PartTimeWorkDays,
                    CompanyPhoneNumber = result.CompanyPhoneNumber,
                    CompanyEmail = result.CompanyEmail,
                    ImageUrl = GetImageUrl(user.ImageUrl),
                    Role = result.Role,
                    IsMale = result.IsMale,
                    IsPending = result.IsPending,
                    Birthday = result.Birthday,
                    WorkType = result.WorkType,
                    WorkFromHomeDays = result.WorkFromHomeDays,
                    IsDisabled = result.IsDisabled,
                    SickLeaveBalance = result.SickLeaveBalance,
                    BankInfo = result.BankInfo,
                    Educations = result.Educations,
                    Attachments = result.Attachments.Select(a => new EmployeeAttachmentResponse
                    {
                        Id = a.Id,
                        OriginalFileName = a.OriginalFileName,
                        ContentType = a.ContentType,
                        FileSize = a.FileSize,
                        UploadedAt = a.UploadedAt,
                        FilePath = a.FilePath,
                        FileUrl = GetImageUrl(a.FilePath)
                    }).ToList()
                };
            }
            
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
                return NotFound();
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPost("addemployee")]
    [Authorize(Roles = "HR,Admin,SuperAdmin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuthResponse>> AddEmployee([FromForm] AddEmployeeForm request, CancellationToken ct)
    {
        try
        {
            var result = await _authService.AddEmployeeAsync(request, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPatch("employee/{id}/active")]
    [Authorize(Roles = "HR,Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ChangeEmployeeActiveStatus(Guid id, [FromBody] ChangeActiveRequest request, CancellationToken ct)
    {
        try
        {
            await _authService.ChangeEmployeeActiveStatusAsync(id, request.IsActive, ct);
            return Ok(new { message = $"Employee status has been {(request.IsActive ? "activated" : "deactivated")} successfully." });
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
                return NotFound();
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpDelete("employee/{id}")]
    [Authorize(Roles = "HR,Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteEmployee(Guid id, CancellationToken ct)
    {
        try
        {
            await _authService.DeleteEmployeeAsync(id, ct);
            return Ok(new { message = "Employee has been deleted successfully." });
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
                return NotFound();
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPost("employee/{id}/salary")]
    [Authorize(Roles = "HR,Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ChangeSalary(Guid id, [FromBody] ChangeSalaryRequest request, CancellationToken ct)
    {
        try
        {
            await _authService.ChangeSalaryAsync(id, request, ct);
            return Ok(new { message = "Salary has been updated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
                return NotFound();
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPost("employee/{id}/allowances")]
    [Authorize(Roles = "HR,Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateAllowances(Guid id, [FromBody] UpdateAllowancesRequest request, CancellationToken ct)
    {
        try
        {
            await _authService.UpdateAllowancesAsync(id, request, ct);
            return Ok(new { message = "Allowances have been updated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
                return NotFound();
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPost("employee/{id}/transfer")]
    [Authorize(Roles = "HR,Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> TransferDepartment(Guid id, [FromBody] TransferDepartmentRequest request, CancellationToken ct)
    {
        try
        {
            await _authService.TransferDepartmentAsync(id, request, ct);
            return Ok(new { message = "Employee has been transferred to the new department successfully." });
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
                return NotFound();
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpPost("employee/{id}/job-title")]
    [Authorize(Roles = "HR,Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ChangeJobTitle(Guid id, [FromBody] ChangeJobTitleRequest request, CancellationToken ct)
    {
        try
        {
            await _authService.ChangeJobTitleAsync(id, request, ct);
            return Ok(new { message = "Employee job title has been updated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
                return NotFound();
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpDelete("employee/{id}/image")]
    [Authorize(Roles = "HR,Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteEmployeeImage(Guid id, CancellationToken ct)
    {
        try
        {
            await _authService.DeleteEmployeeImageAsync(id, ct);
            return Ok(new { message = "Employee image has been deleted successfully." });
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
                return NotFound();
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpDelete("employee/{employeeId}/attachment/{attachmentId}")]
    [Authorize(Roles = "HR,Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteEmployeeAttachment(Guid employeeId, Guid attachmentId, CancellationToken ct)
    {
        try
        {
            // Verify employee exists
            var employee = await _authService.GetUserByIdAsync(employeeId, ct);
            if (employee == null)
                return NotFound(new { message = "Employee not found." });

            var deleted = await _attachmentService.DeleteAttachmentByAdminAsync(attachmentId, ct);
            if (!deleted)
                return NotFound(new { message = "Attachment not found." });

            return Ok(new { message = "Attachment has been deleted successfully." });
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: ex.Message);
        }
    }

    [HttpGet("employees/import-template")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmployeesImportTemplate(CancellationToken ct)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("EmployeesTemplate");

        var headers = GetEmployeeImportHeaders();
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cells[1, i + 1].Value = headers[i];
            ws.Cells[1, i + 1].Style.Font.Bold = true;
        }

        // Dropdown lists for name-based reference columns
        var listsSheet = package.Workbook.Worksheets.Add("Lists");
        listsSheet.Hidden = eWorkSheetHidden.Hidden;

        var departments = await _db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync(ct);
        var jobTitles = await _db.JobTitles.AsNoTracking().OrderBy(j => j.Name).ToListAsync(ct);
        var branches = await _db.Branches.AsNoTracking().OrderBy(b => b.Name).ToListAsync(ct);
        var employmentModes = await _db.EmploymentModes.AsNoTracking().OrderBy(e => e.Name).ToListAsync(ct);
        var governorates = await _db.Governorates.AsNoTracking().OrderBy(g => g.Name).ToListAsync(ct);
        var cities = await _db.Cities.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);
        var nationalities = await _db.Nationalities.AsNoTracking().OrderBy(n => n.Name).ToListAsync(ct);
        var maritalStatuses = await _db.MaritalStatuses.AsNoTracking().OrderBy(m => m.Name).ToListAsync(ct);
        var insuranceCompanies = await _db.InsuranceCompanies.AsNoTracking().OrderBy(i => i.Name).ToListAsync(ct);

        int listCol = 1;
        WriteListColumn(listsSheet, listCol++, "DepartmentName", departments.Select(d => d.Name));
        WriteListColumn(listsSheet, listCol++, "JobTitleName", jobTitles.Select(j => j.Name));
        WriteListColumn(listsSheet, listCol++, "BranchName", branches.Select(b => b.Name));
        WriteListColumn(listsSheet, listCol++, "EmploymentModeName", employmentModes.Select(e => e.Name));
        WriteListColumn(listsSheet, listCol++, "GovernorateName", governorates.Select(g => g.Name));
        WriteListColumn(listsSheet, listCol++, "CityName", cities.Select(c => c.Name));
        WriteListColumn(listsSheet, listCol++, "NationalityName", nationalities.Select(n => n.Name));
        WriteListColumn(listsSheet, listCol++, "MaritalStatusName", maritalStatuses.Select(m => m.Name));
        WriteListColumn(listsSheet, listCol++, "InsuranceCompanyName", insuranceCompanies.Select(i => i.Name));
        WriteListColumn(listsSheet, listCol++, "IsMale", new[] { "true", "false" });
        WriteListColumn(listsSheet, listCol++, "IsInsured", new[] { "true", "false" });
        WriteListColumn(listsSheet, listCol++, "WorkType", Enum.GetNames<internalEmployee.Data.Entities.WorkType>());

        const int dataStartRow = 2;
        const int dataEndRow = 1000;
        AddListValidation(ws, "DepartmentName", listsSheet, "DepartmentName", dataStartRow, dataEndRow);
        AddListValidation(ws, "JobTitleName", listsSheet, "JobTitleName", dataStartRow, dataEndRow);
        AddListValidation(ws, "BranchName", listsSheet, "BranchName", dataStartRow, dataEndRow);
        AddListValidation(ws, "EmploymentModeName", listsSheet, "EmploymentModeName", dataStartRow, dataEndRow);
        AddListValidation(ws, "GovernorateName", listsSheet, "GovernorateName", dataStartRow, dataEndRow);
        AddListValidation(ws, "CityName", listsSheet, "CityName", dataStartRow, dataEndRow);
        AddListValidation(ws, "NationalityName", listsSheet, "NationalityName", dataStartRow, dataEndRow);
        AddListValidation(ws, "MaritalStatusName", listsSheet, "MaritalStatusName", dataStartRow, dataEndRow);
        AddListValidation(ws, "InsuranceCompanyName", listsSheet, "InsuranceCompanyName", dataStartRow, dataEndRow);
        AddListValidation(ws, "IsMale", listsSheet, "IsMale", dataStartRow, dataEndRow);
        AddListValidation(ws, "IsInsured", listsSheet, "IsInsured", dataStartRow, dataEndRow);
        AddListValidation(ws, "WorkType", listsSheet, "WorkType", dataStartRow, dataEndRow);

        ws.Cells[ws.Dimension.Address].AutoFitColumns();

        var bytes = package.GetAsByteArray();
        var fileName = $"Employees_Import_Template_{DateTime.Now:yyyyMMdd}.xlsx";
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpPost("employees/import-excel")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportEmployeesFromExcel([FromForm] ImportEmployeesForm importForm, CancellationToken ct)
    {
        if (importForm.File == null || importForm.File.Length == 0)
            return BadRequest("File is required.");

        var fileName = importForm.File.FileName ?? string.Empty;
        var extension = Path.GetExtension(fileName);
        var isCsv = extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
            || (importForm.File.ContentType?.Contains("csv", StringComparison.OrdinalIgnoreCase) ?? false);

        if (isCsv)
        {
            using var csvStream = importForm.File.OpenReadStream();
            return await ImportEmployeesFromCsv(csvStream, ct);
        }

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var results = new List<object>();
        int added = 0;
        int updated = 0;

        // Build lookup maps (by name) for reference data
        var departments = await _db.Departments.AsNoTracking().ToListAsync(ct);
        var jobTitles = await _db.JobTitles.AsNoTracking().ToListAsync(ct);
        var branches = await _db.Branches.AsNoTracking().ToListAsync(ct);
        var employmentModes = await _db.EmploymentModes.AsNoTracking().ToListAsync(ct);
        var governorates = await _db.Governorates.AsNoTracking().ToListAsync(ct);
        var cities = await _db.Cities.AsNoTracking().ToListAsync(ct);
        var nationalities = await _db.Nationalities.AsNoTracking().ToListAsync(ct);
        var maritalStatuses = await _db.MaritalStatuses.AsNoTracking().ToListAsync(ct);
        var insuranceCompanies = await _db.InsuranceCompanies.AsNoTracking().ToListAsync(ct);
        var users = await _db.Users.AsNoTracking().ToListAsync(ct);

        var departmentMap = BuildNameMap(departments.Select(d => (d.Id, d.Name, (string?)null)));
        var jobTitleMap = BuildNameMap(jobTitles.Select(j => (j.Id, j.Name, j.NameAr)));
        var branchMap = BuildNameMap(branches.Select(b => (b.Id, b.Name, b.NameAr)));
        var employmentModeMap = BuildNameMap(employmentModes.Select(e => (e.Id, e.Name, e.NameAr)));
        var governorateMap = BuildNameMap(governorates.Select(g => (g.Id, g.Name, g.NameAr)));
        var cityMap = BuildNameMap(cities.Select(c => (c.Id, c.Name, c.NameAr)));
        var nationalityMap = BuildNameMap(nationalities.Select(n => (n.Id, n.Name, n.NameAr)));
        var maritalStatusMap = BuildNameMap(maritalStatuses.Select(m => (m.Id, m.Name, m.NameAr)));
        var insuranceCompanyMap = BuildNameMap(insuranceCompanies.Select(i => (i.Id, i.Name, i.NameAr)));

        var usersByNationalId = BuildUserIndex(users, u => u.NationalId);
        var usersByMachineCode = BuildUserIndex(users, u => u.MachineCode);
        var usersByPhone = BuildUserIndex(users, u => u.PhoneNumber);
        var usersByEmployeeCode = BuildUserIndex(users, u => u.EmployeeCode);
        var usedEmployeeCodes = new HashSet<string>(
            users.Where(u => !string.IsNullOrWhiteSpace(u.EmployeeCode))
                .Select(u => u.EmployeeCode!.Trim()),
            StringComparer.OrdinalIgnoreCase);

        using var stream = new MemoryStream();
        await importForm.File.CopyToAsync(stream, ct);
        using var package = new ExcelPackage(stream);

        var ws = package.Workbook.Worksheets.FirstOrDefault();
        if (ws?.Dimension == null)
            return BadRequest("Excel sheet is empty.");

        var headerMap = BuildHeaderMap(ws);
        var lastRow = ws.Dimension.End.Row;

        for (int row = 2; row <= lastRow; row++)
        {
            if (IsRowEmpty(ws, row, headerMap.Values))
                continue;

            try
            {
                var departmentName = GetString(ws, row, headerMap, "DepartmentName");
                var jobTitleName = GetString(ws, row, headerMap, "JobTitleName");

                var departmentId = await ResolveOrCreateDepartmentIdAsync(departmentName, departmentMap, ct);
                var jobId = await ResolveOrCreateJobTitleIdAsync(jobTitleName, jobTitleMap, ct);
                var branchId = ResolveId(GetString(ws, row, headerMap, "BranchName"), branchMap, "BranchName");
                var employmentModeId = ResolveId(GetString(ws, row, headerMap, "EmploymentModeName"), employmentModeMap, "EmploymentModeName");
                var governorateId = ResolveId(GetString(ws, row, headerMap, "GovernorateName"), governorateMap, "GovernorateName");
                var cityId = ResolveId(GetString(ws, row, headerMap, "CityName"), cityMap, "CityName");
                var nationalityId = ResolveId(GetString(ws, row, headerMap, "NationalityName"), nationalityMap, "NationalityName");
                var maritalStatusId = ResolveId(GetString(ws, row, headerMap, "MaritalStatusName"), maritalStatusMap, "MaritalStatusName");
                var insuranceCompanyId = ResolveId(GetString(ws, row, headerMap, "InsuranceCompanyName"), insuranceCompanyMap, "InsuranceCompanyName");

                var nationalId = GetString(ws, row, headerMap, "NationalId");
                var phoneNumber = NormalizePhone(GetString(ws, row, headerMap, "PhoneNumber"));
                var machineCode = GetString(ws, row, headerMap, "MachineCode");
                var employeeCode = GetString(ws, row, headerMap, "EmployeeCode");
                var roleText = GetString(ws, row, headerMap, "Role");

                var existing = FindExistingUser(
                    nationalId,
                    machineCode,
                    phoneNumber,
                    employeeCode,
                    usersByNationalId,
                    usersByMachineCode,
                    usersByPhone,
                    usersByEmployeeCode);

                if (existing != null)
                {
                    var update = new UpdateEmployeeForm();
                    if (!string.IsNullOrWhiteSpace(nationalId)) update.NationalId = nationalId;
                    if (!string.IsNullOrWhiteSpace(phoneNumber)) update.PhoneNumber = phoneNumber;
                    if (!string.IsNullOrWhiteSpace(employeeCode)) update.EmployeeCode = employeeCode;

                    update.FirstNameAr = GetString(ws, row, headerMap, "FirstNameAr");
                    update.MiddleNameAr = GetString(ws, row, headerMap, "MiddleNameAr");
                    update.LastNameAr = GetString(ws, row, headerMap, "LastNameAr");
                    update.FirstNameEn = GetString(ws, row, headerMap, "FirstNameEn");
                    update.MiddleNameEn = GetString(ws, row, headerMap, "MiddleNameEn");
                    update.LastNameEn = GetString(ws, row, headerMap, "LastNameEn");

                    update.IsMale = GetBool(ws, row, headerMap, "IsMale");
                    update.NationalityId = nationalityId;
                    update.Religion = GetEnumNullable<internalEmployee.Auth.Models.Religion>(ws, row, headerMap, "Religion");
                    update.AllowMobileAttendanceFromAnyLocation =
                        GetBool(ws, row, headerMap, "AllowMobileAttendanceFromAnyLocation")
                        ?? GetBool(ws, row, headerMap, "Allow Mobile Attendance From Any Location");
                    update.MaritalStatusId = maritalStatusId;
                    update.Birthday = GetDateOnly(ws, row, headerMap, "Birthday");
                    update.Email = GetString(ws, row, headerMap, "Email");
                    update.HomePhone = GetString(ws, row, headerMap, "HomePhone");
                    update.AddressAr = GetString(ws, row, headerMap, "AddressAr");
                    update.AddressEn = GetString(ws, row, headerMap, "AddressEn");
                    update.GovernorateId = governorateId;
                    update.CityId = cityId;
                    update.DepartmentId = departmentId;
                    update.BranchId = branchId;
                    update.JobId = jobId;
                    update.EmploymentModeId = employmentModeId;
                    update.StartDate = GetDateOnly(ws, row, headerMap, "StartDate");
                    update.ContractEndDate = GetDateOnly(ws, row, headerMap, "ContractEndDate");
                    update.GrossSalary = GetDecimal(ws, row, headerMap, "GrossSalary");
                    update.ShiftRate = GetDecimal(ws, row, headerMap, "ShiftRate") 
                        ?? GetDecimal(ws, row, headerMap, "Shift Rate");
                    update.HousingAllowance = GetDecimal(ws, row, headerMap, "HousingAllowance");
                    update.MealAllowance = GetDecimal(ws, row, headerMap, "MealAllowance");
                    update.TransportationAllowance = GetDecimal(ws, row, headerMap, "TransportationAllowance");
                    update.InsuranceAllowance = GetDecimal(ws, row, headerMap, "InsuranceAllowance");
                    update.OvertimeRate = GetDecimal(ws, row, headerMap, "OvertimeRate");
                    update.InsuranceSalary = GetDecimal(ws, row, headerMap, "InsuranceSalary");
                    update.IsInsured = GetBool(ws, row, headerMap, "IsInsured");
                    update.InsuranceCompanyId = insuranceCompanyId;
                    update.SickLeaveBalance = GetDecimal(ws, row, headerMap, "SickLeaveBalance");
                    update.WorkType = GetEnumNullable<internalEmployee.Data.Entities.WorkType>(ws, row, headerMap, "WorkType");
                    update.WorkFromHomeDays = GetDaysOfWeek(ws, row, headerMap, "WorkFromHomeDays");
                    update.PartTimeStart = GetTimeOnly(ws, row, headerMap, "PartTimeStart");
                    update.PartTimeEnd = GetTimeOnly(ws, row, headerMap, "PartTimeEnd");
                    update.CompanyEmail = GetString(ws, row, headerMap, "CompanyEmail");
                    update.CompanyPhoneNumber = GetString(ws, row, headerMap, "CompanyPhoneNumber");
                    update.BankName = GetString(ws, row, headerMap, "BankName");
                    update.AccountNumber = GetString(ws, row, headerMap, "AccountNumber");
                    update.IbanNumber = GetString(ws, row, headerMap, "IbanNumber");
                    update.SwiftBicCode = GetString(ws, row, headerMap, "SwiftBicCode");
                    update.BankBranchCode = GetString(ws, row, headerMap, "BankBranchCode");
                    update.MachineCode = machineCode;

                    await _authService.UpdateEmployeeAsync(existing.Id, update, ct);
                    updated++;
                }
                else
                {
                    var password = GetString(ws, row, headerMap, "Password");
                    if (string.IsNullOrWhiteSpace(password))
                        password = "ChangeMe123!";

                    if (string.IsNullOrWhiteSpace(employeeCode))
                        employeeCode = GenerateUniqueEmployeeCode(usedEmployeeCodes, machineCode, nationalId, phoneNumber, row);

                    var role = ParseRole(roleText);
                    EnsureRoleAllowedForCaller(role);

                    var form = new AddEmployeeForm
                    {
                        NationalId = nationalId,
                        Password = password ?? string.Empty,
                        FirstNameAr = GetString(ws, row, headerMap, "FirstNameAr"),
                        MiddleNameAr = GetString(ws, row, headerMap, "MiddleNameAr"),
                        LastNameAr = GetString(ws, row, headerMap, "LastNameAr"),
                        FirstNameEn = GetString(ws, row, headerMap, "FirstNameEn"),
                        MiddleNameEn = GetString(ws, row, headerMap, "MiddleNameEn"),
                        LastNameEn = GetString(ws, row, headerMap, "LastNameEn"),
                        IsMale = GetBool(ws, row, headerMap, "IsMale") ?? false,
                        NationalityId = nationalityId,
                        Religion = GetEnumNullable<internalEmployee.Auth.Models.Religion>(ws, row, headerMap, "Religion"),
                        AllowMobileAttendanceFromAnyLocation =
                            GetBool(ws, row, headerMap, "AllowMobileAttendanceFromAnyLocation")
                            ?? GetBool(ws, row, headerMap, "Allow Mobile Attendance From Any Location")
                            ?? false,
                        MaritalStatusId = maritalStatusId,
                        Birthday = GetDateOnly(ws, row, headerMap, "Birthday"),
                        PhoneNumber = phoneNumber ?? string.Empty,
                        Email = GetString(ws, row, headerMap, "Email"),
                        HomePhone = GetString(ws, row, headerMap, "HomePhone"),
                        AddressAr = GetString(ws, row, headerMap, "AddressAr"),
                        AddressEn = GetString(ws, row, headerMap, "AddressEn"),
                        GovernorateId = governorateId,
                        CityId = cityId,
                        DepartmentId = departmentId,
                        BranchId = branchId,
                        JobId = jobId,
                        EmploymentModeId = employmentModeId,
                        StartDate = GetDateOnly(ws, row, headerMap, "StartDate"),
                        ContractEndDate = GetDateOnly(ws, row, headerMap, "ContractEndDate"),
                        GrossSalary = GetDecimal(ws, row, headerMap, "GrossSalary"),
                        ShiftRate = GetDecimal(ws, row, headerMap, "ShiftRate") 
                            ?? GetDecimal(ws, row, headerMap, "Shift Rate"),
                        HousingAllowance = GetDecimal(ws, row, headerMap, "HousingAllowance"),
                        MealAllowance = GetDecimal(ws, row, headerMap, "MealAllowance"),
                        TransportationAllowance = GetDecimal(ws, row, headerMap, "TransportationAllowance"),
                        InsuranceAllowance = GetDecimal(ws, row, headerMap, "InsuranceAllowance"),
                        OvertimeRate = GetDecimal(ws, row, headerMap, "OvertimeRate"),
                        InsuranceSalary = GetDecimal(ws, row, headerMap, "InsuranceSalary"),
                        IsInsured = GetBool(ws, row, headerMap, "IsInsured") ?? false,
                        InsuranceCompanyId = insuranceCompanyId,
                        SickLeaveBalance = GetDecimal(ws, row, headerMap, "SickLeaveBalance"),
                        WorkType = GetEnumOrDefault(internalEmployee.Data.Entities.WorkType.Onsite, ws, row, headerMap, "WorkType"),
                        WorkFromHomeDays = GetDaysOfWeek(ws, row, headerMap, "WorkFromHomeDays"),
                        PartTimeStart = GetTimeOnly(ws, row, headerMap, "PartTimeStart"),
                        PartTimeEnd = GetTimeOnly(ws, row, headerMap, "PartTimeEnd"),
                        CompanyEmail = GetString(ws, row, headerMap, "CompanyEmail"),
                        CompanyPhoneNumber = GetString(ws, row, headerMap, "CompanyPhoneNumber"),
                        BankName = GetString(ws, row, headerMap, "BankName"),
                        AccountNumber = GetString(ws, row, headerMap, "AccountNumber"),
                        IbanNumber = GetString(ws, row, headerMap, "IbanNumber"),
                        SwiftBicCode = GetString(ws, row, headerMap, "SwiftBicCode"),
                        BankBranchCode = GetString(ws, row, headerMap, "BankBranchCode"),
                        MachineCode = machineCode,
                        EmployeeCode = employeeCode,
                        Role = role
                    };

                    if (string.IsNullOrWhiteSpace(form.Password))
                        throw new InvalidOperationException("Password is required.");
                    if (string.IsNullOrWhiteSpace(form.PhoneNumber))
                        throw new InvalidOperationException("PhoneNumber is required.");

                    await _authService.AddEmployeeAsync(form, ct);
                    added++;
                }
            }
            catch (Exception ex)
            {
                var detail = ex.GetBaseException().Message;
                results.Add(new { Row = row, Error = detail });
            }
        }

        return Ok(new
        {
            Added = added,
            Updated = updated,
            Failed = results.Count,
            Errors = results
        });
    }

    [HttpGet("employees/export-excel")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportAllEmployeesToExcel(CancellationToken ct)
    {
        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Load ALL users (all roles) — GetAllEmployeesWithDetailsAsync only returns AppRole.User
            var allUserIds = await _db.Users
                .AsNoTracking()
                .OrderBy(u => u.FirstNameEn ?? u.FirstNameAr ?? u.NationalId ?? u.PassportNumber)
                .Select(u => u.Id)
                .ToListAsync(ct);

            var employees = new List<EmployeeDetailsResponse>(allUserIds.Count);
            foreach (var uid in allUserIds)
            {
                var detail = await _authService.GetEmployeeByIdWithDetailsAsync(uid, ct);
                if (detail != null)
                    employees.Add(detail);
            }

            // Map attachment URLs to absolute
            foreach (var emp in employees)
            {
                foreach (var att in emp.Attachments)
                {
                    att.FileUrl = GetImageUrl(att.FilePath);
                }
            }
            employees = employees.Where(e => e.IsActive).ToList();

            using var package = new ExcelPackage();

            // ─── Color Palette ───
            var brandBlue = Color.FromArgb(30, 42, 120);        // #1E2A78
            var headerBg = Color.FromArgb(30, 42, 120);
            var headerFont = Color.White;
            var subHeaderBg = Color.FromArgb(232, 236, 241);     // light grey-blue
            var altRowBg = Color.FromArgb(245, 247, 250);        // very light grey
            var borderColor = Color.FromArgb(200, 210, 220);

            // ═══════════════════════════════════════════════════
            // SHEET 1 — Employees
            // ═══════════════════════════════════════════════════
            var ws = package.Workbook.Worksheets.Add("Employees");

            var empHeaders = new[]
            {
                "#", "Full Name", "Department", "Job Title", "Hiring Date",
                "National ID", "Passport Number",
                "Email", "Phone Number", "Company Email", "Company Phone",
                "Gender", "Religion", "Allow Mobile Attendance From Any Location", "Birthday",
                "Address",
                "Role",
                "Contract End Date",
                "Gross Salary", "Shift Rate", "Housing Allowance", "Meal Allowance",
                "Transportation Allowance", "Insurance Allowance", "Overtime Rate", "Insurance Salary",
                "Sick Leave Balance",
                "Machine Code"
            };

            // Title row
            ws.Cells["A1"].Value = "Employee Data Export";
            ws.Cells[1, 1, 1, empHeaders.Length].Merge = true;
            ws.Cells["A1"].Style.Font.Size = 16;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.Font.Color.SetColor(headerFont);
            ws.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells["A1"].Style.Fill.BackgroundColor.SetColor(brandBlue);
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Row(1).Height = 35;
            ws.Cells["A1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            // Subtitle row
            ws.Cells["A2"].Value = $"Generated on: {DateTime.Now:dd/MM/yyyy HH:mm} UTC  •  Total Employees: {employees.Count}";
            ws.Cells[2, 1, 2, empHeaders.Length].Merge = true;
            ws.Cells["A2"].Style.Font.Size = 10;
            ws.Cells["A2"].Style.Font.Italic = true;
            ws.Cells["A2"].Style.Font.Color.SetColor(Color.FromArgb(100, 100, 100));
            ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Header row
            for (int i = 0; i < empHeaders.Length; i++)
            {
                var cell = ws.Cells[3, i + 1];
                cell.Value = empHeaders[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.Color.SetColor(headerFont);
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(headerBg);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                cell.Style.Border.Bottom.Color.SetColor(borderColor);
            }
            ws.Row(3).Height = 28;

            // Data rows
            for (int i = 0; i < employees.Count; i++)
            {
                var e = employees[i];
                var fullNameAr = string.Join(" ", new[] { e.FirstNameAr, e.MiddleNameAr, e.LastNameAr }
                    .Where(n => !string.IsNullOrWhiteSpace(n)));
                var fullNameEn = string.Join(" ", new[] { e.FirstNameEn, e.MiddleNameEn, e.LastNameEn }
                    .Where(n => !string.IsNullOrWhiteSpace(n)));
                var fullName = !string.IsNullOrWhiteSpace(fullNameAr) ? fullNameAr : fullNameEn;
                var address = string.Join(" - ", new[] { e.GovernorateName, e.CityName, e.AddressAr }
                    .Where(n => !string.IsNullOrWhiteSpace(n)));
                var grossSalary = (e.GrossSalary ?? 0m)
                    + (e.HousingAllowance ?? 0m)
                    + (e.MealAllowance ?? 0m)
                    + (e.TransportationAllowance ?? 0m)
                    + (e.InsuranceAllowance ?? 0m);

                int row = i + 4;
                int col = 1;

                ws.Cells[row, col++].Value = i + 1;
                ws.Cells[row, col++].Value = fullName;
                ws.Cells[row, col++].Value = e.DepartmentName;
                ws.Cells[row, col++].Value = e.JobTitleName;
                ws.Cells[row, col++].Value = e.StartDate?.ToString("dd/MM/yyyy");
                ws.Cells[row, col++].Value = e.NationalId;
                ws.Cells[row, col++].Value = e.PassportNumber;
                ws.Cells[row, col++].Value = e.Email;
                ws.Cells[row, col++].Value = e.PhoneNumber;
                ws.Cells[row, col++].Value = e.CompanyEmail;
                ws.Cells[row, col++].Value = e.CompanyPhoneNumber;
                ws.Cells[row, col++].Value = e.IsMale ? "Male" : "Female";
                ws.Cells[row, col++].Value = e.Religion?.ToString();
                ws.Cells[row, col++].Value = e.AllowMobileAttendanceFromAnyLocation;
                ws.Cells[row, col++].Value = e.Birthday?.ToString("dd/MM/yyyy");
                ws.Cells[row, col++].Value = address;
                ws.Cells[row, col++].Value = e.Role;
                ws.Cells[row, col++].Value = e.ContractEndDate?.ToString("dd/MM/yyyy");
                ws.Cells[row, col++].Value = grossSalary;
                ws.Cells[row, col++].Value = e.ShiftRate;
                ws.Cells[row, col++].Value = e.HousingAllowance;
                ws.Cells[row, col++].Value = e.MealAllowance;
                ws.Cells[row, col++].Value = e.TransportationAllowance;
                ws.Cells[row, col++].Value = e.InsuranceAllowance;
                ws.Cells[row, col++].Value = e.OvertimeRate;
                ws.Cells[row, col++].Value = e.InsuranceSalary;
                ws.Cells[row, col++].Value = e.SickLeaveBalance;
                ws.Cells[row, col++].Value = e.MachineCode;

                // Alternating row color
                if (i % 2 == 1)
                {
                    ws.Cells[row, 1, row, empHeaders.Length].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[row, 1, row, empHeaders.Length].Style.Fill.BackgroundColor.SetColor(altRowBg);
                }
            }

            // Format salary columns as currency
            if (employees.Count > 0)
            {
                ws.Cells[4, 17, employees.Count + 3, 24].Style.Numberformat.Format = "#,##0.00";
            }

            // Auto-fit and freeze panes
            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            ws.View.FreezePanes(4, 1); // Freeze header rows

            // ═══════════════════════════════════════════════════
            // SHEET 2 — Education
            // ═══════════════════════════════════════════════════
            var wsEdu = package.Workbook.Worksheets.Add("Education");

            var eduHeaders = new[]
            {
                "#", "Employee Code", "Employee Name (EN)", "Employee Name (AR)",
                "University Name", "Degree", "Graduation Year", "Final Grade", "Created At"
            };

            // Title
            wsEdu.Cells["A1"].Value = "Employee Education Records";
            wsEdu.Cells[1, 1, 1, eduHeaders.Length].Merge = true;
            wsEdu.Cells["A1"].Style.Font.Size = 16;
            wsEdu.Cells["A1"].Style.Font.Bold = true;
            wsEdu.Cells["A1"].Style.Font.Color.SetColor(headerFont);
            wsEdu.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            wsEdu.Cells["A1"].Style.Fill.BackgroundColor.SetColor(brandBlue);
            wsEdu.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            wsEdu.Row(1).Height = 35;
            wsEdu.Cells["A1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            for (int i = 0; i < eduHeaders.Length; i++)
            {
                var cell = wsEdu.Cells[2, i + 1];
                cell.Value = eduHeaders[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.Color.SetColor(headerFont);
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(headerBg);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }
            wsEdu.Row(2).Height = 28;

            int eduRow = 3;
            int eduIndex = 1;
            foreach (var emp in employees)
            {
                var fullNameEn = string.Join(" ", new[] { emp.FirstNameEn, emp.MiddleNameEn, emp.LastNameEn }
                    .Where(n => !string.IsNullOrWhiteSpace(n)));
                var fullNameAr = string.Join(" ", new[] { emp.FirstNameAr, emp.MiddleNameAr, emp.LastNameAr }
                    .Where(n => !string.IsNullOrWhiteSpace(n)));

                foreach (var edu in emp.Educations)
                {
                    wsEdu.Cells[eduRow, 1].Value = eduIndex++;
                    wsEdu.Cells[eduRow, 2].Value = emp.EmployeeCode;
                    wsEdu.Cells[eduRow, 3].Value = fullNameEn;
                    wsEdu.Cells[eduRow, 4].Value = fullNameAr;
                    wsEdu.Cells[eduRow, 5].Value = edu.UniversityName;
                    wsEdu.Cells[eduRow, 6].Value = edu.Degree;
                    wsEdu.Cells[eduRow, 7].Value = edu.GraduationYear?.ToString("yyyy");
                    wsEdu.Cells[eduRow, 8].Value = edu.FinalGrade;
                    wsEdu.Cells[eduRow, 9].Value = edu.CreatedAt.ToString("dd/MM/yyyy");

                    if ((eduRow - 3) % 2 == 1)
                    {
                        wsEdu.Cells[eduRow, 1, eduRow, eduHeaders.Length].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        wsEdu.Cells[eduRow, 1, eduRow, eduHeaders.Length].Style.Fill.BackgroundColor.SetColor(altRowBg);
                    }
                    eduRow++;
                }
            }

            if (wsEdu.Dimension != null)
                wsEdu.Cells[wsEdu.Dimension.Address].AutoFitColumns();
            wsEdu.View.FreezePanes(3, 1);

            // ═══════════════════════════════════════════════════
            // SHEET 3 — Bank Info
            // ═══════════════════════════════════════════════════
            var wsBank = package.Workbook.Worksheets.Add("Bank Info");

            var bankHeaders = new[]
            {
                "#", "Employee Code", "Employee Name (EN)", "Employee Name (AR)",
                "Bank Name", "Account Number", "IBAN Number", "SWIFT/BIC Code", "Branch Code"
            };

            // Title
            wsBank.Cells["A1"].Value = "Employee Bank Information";
            wsBank.Cells[1, 1, 1, bankHeaders.Length].Merge = true;
            wsBank.Cells["A1"].Style.Font.Size = 16;
            wsBank.Cells["A1"].Style.Font.Bold = true;
            wsBank.Cells["A1"].Style.Font.Color.SetColor(headerFont);
            wsBank.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            wsBank.Cells["A1"].Style.Fill.BackgroundColor.SetColor(brandBlue);
            wsBank.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            wsBank.Row(1).Height = 35;
            wsBank.Cells["A1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            for (int i = 0; i < bankHeaders.Length; i++)
            {
                var cell = wsBank.Cells[2, i + 1];
                cell.Value = bankHeaders[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.Color.SetColor(headerFont);
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(headerBg);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }
            wsBank.Row(2).Height = 28;

            int bankRow = 3;
            int bankIndex = 1;
            foreach (var emp in employees)
            {
                if (emp.BankInfo == null) continue;

                var fullNameEn = string.Join(" ", new[] { emp.FirstNameEn, emp.MiddleNameEn, emp.LastNameEn }
                    .Where(n => !string.IsNullOrWhiteSpace(n)));
                var fullNameAr = string.Join(" ", new[] { emp.FirstNameAr, emp.MiddleNameAr, emp.LastNameAr }
                    .Where(n => !string.IsNullOrWhiteSpace(n)));

                wsBank.Cells[bankRow, 1].Value = bankIndex++;
                wsBank.Cells[bankRow, 2].Value = emp.EmployeeCode;
                wsBank.Cells[bankRow, 3].Value = fullNameEn;
                wsBank.Cells[bankRow, 4].Value = fullNameAr;
                wsBank.Cells[bankRow, 5].Value = emp.BankInfo.BankName;
                wsBank.Cells[bankRow, 6].Value = emp.BankInfo.AccountNumber;
                wsBank.Cells[bankRow, 7].Value = emp.BankInfo.IbanNumber;
                wsBank.Cells[bankRow, 8].Value = emp.BankInfo.SwiftBicCode;
                wsBank.Cells[bankRow, 9].Value = emp.BankInfo.BranchCode;

                if ((bankRow - 3) % 2 == 1)
                {
                    wsBank.Cells[bankRow, 1, bankRow, bankHeaders.Length].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    wsBank.Cells[bankRow, 1, bankRow, bankHeaders.Length].Style.Fill.BackgroundColor.SetColor(altRowBg);
                }
                bankRow++;
            }

            if (wsBank.Dimension != null)
                wsBank.Cells[wsBank.Dimension.Address].AutoFitColumns();
            wsBank.View.FreezePanes(3, 1);

            // ═══════════════════════════════════════════════════
            // SHEET 4 — Attachments
            // ═══════════════════════════════════════════════════
            var wsAtt = package.Workbook.Worksheets.Add("Attachments");

            var attHeaders = new[]
            {
                "#", "Employee Code", "Employee Name (EN)", "Employee Name (AR)",
                "File Name", "Content Type", "File Size (KB)", "Uploaded At", "Download URL"
            };

            // Title
            wsAtt.Cells["A1"].Value = "Employee Attachments";
            wsAtt.Cells[1, 1, 1, attHeaders.Length].Merge = true;
            wsAtt.Cells["A1"].Style.Font.Size = 16;
            wsAtt.Cells["A1"].Style.Font.Bold = true;
            wsAtt.Cells["A1"].Style.Font.Color.SetColor(headerFont);
            wsAtt.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            wsAtt.Cells["A1"].Style.Fill.BackgroundColor.SetColor(brandBlue);
            wsAtt.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            wsAtt.Row(1).Height = 35;
            wsAtt.Cells["A1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            for (int i = 0; i < attHeaders.Length; i++)
            {
                var cell = wsAtt.Cells[2, i + 1];
                cell.Value = attHeaders[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.Color.SetColor(headerFont);
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(headerBg);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }
            wsAtt.Row(2).Height = 28;

            int attRow = 3;
            int attIndex = 1;
            foreach (var emp in employees)
            {
                var fullNameEn = string.Join(" ", new[] { emp.FirstNameEn, emp.MiddleNameEn, emp.LastNameEn }
                    .Where(n => !string.IsNullOrWhiteSpace(n)));
                var fullNameAr = string.Join(" ", new[] { emp.FirstNameAr, emp.MiddleNameAr, emp.LastNameAr }
                    .Where(n => !string.IsNullOrWhiteSpace(n)));

                foreach (var att in emp.Attachments)
                {
                    wsAtt.Cells[attRow, 1].Value = attIndex++;
                    wsAtt.Cells[attRow, 2].Value = emp.EmployeeCode;
                    wsAtt.Cells[attRow, 3].Value = fullNameEn;
                    wsAtt.Cells[attRow, 4].Value = fullNameAr;
                    wsAtt.Cells[attRow, 5].Value = att.OriginalFileName;
                    wsAtt.Cells[attRow, 6].Value = att.ContentType;
                    wsAtt.Cells[attRow, 7].Value = Math.Round(att.FileSize / 1024.0, 2);
                    wsAtt.Cells[attRow, 8].Value = att.UploadedAt.ToString("dd/MM/yyyy HH:mm");

                    // Add clickable hyperlink for the download URL
                    var downloadUrl = att.FileUrl;
                    if (!string.IsNullOrWhiteSpace(downloadUrl))
                    {
                        wsAtt.Cells[attRow, 9].Value = downloadUrl;
                        wsAtt.Cells[attRow, 9].Style.Font.Color.SetColor(Color.Blue);
                        wsAtt.Cells[attRow, 9].Style.Font.UnderLine = true;
                    }

                    if ((attRow - 3) % 2 == 1)
                    {
                        wsAtt.Cells[attRow, 1, attRow, attHeaders.Length].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        wsAtt.Cells[attRow, 1, attRow, attHeaders.Length].Style.Fill.BackgroundColor.SetColor(altRowBg);
                    }
                    attRow++;
                }
            }

            if (wsAtt.Dimension != null)
                wsAtt.Cells[wsAtt.Dimension.Address].AutoFitColumns();
            wsAtt.View.FreezePanes(3, 1);

            // ─── Return the file ───
            var excelBytes = package.GetAsByteArray();
            var fileName = $"Employees_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: ex.Message);
        }
    }

    private async Task<IActionResult> ImportEmployeesFromCsv(Stream stream, CancellationToken ct)
    {
        var results = new List<object>();
        int added = 0;
        int updated = 0;

        var departments = await _db.Departments.AsNoTracking().ToListAsync(ct);
        var jobTitles = await _db.JobTitles.AsNoTracking().ToListAsync(ct);
        var users = await _db.Users.AsNoTracking().ToListAsync(ct);

        var departmentMap = BuildNameMap(departments.Select(d => (d.Id, d.Name, (string?)null)));
        var jobTitleMap = BuildNameMap(jobTitles.Select(j => (j.Id, j.Name, j.NameAr)));

        var usersByNationalId = BuildUserIndex(users, u => u.NationalId);
        var usersByMachineCode = BuildUserIndex(users, u => u.MachineCode);
        var usersByPhone = BuildUserIndex(users, u => u.PhoneNumber);
        var usersByEmployeeCode = BuildUserIndex(users, u => u.EmployeeCode);
        var usedEmployeeCodes = new HashSet<string>(
            users.Where(u => !string.IsNullOrWhiteSpace(u.EmployeeCode))
                .Select(u => u.EmployeeCode!.Trim()),
            StringComparer.OrdinalIgnoreCase);

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();
        var encoding = DetectCsvEncoding(bytes);
        ms.Position = 0;
        using var reader = new StreamReader(ms, encoding, true);
        using var parser = new TextFieldParser(reader)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = true
        };
        parser.SetDelimiters(",");

        var headers = parser.ReadFields();
        if (headers == null || headers.Length == 0)
            return BadRequest("CSV header row is missing.");

        var headerMap = BuildNormalizedHeaderMap(headers);
        int rowNumber = 1;

        while (!parser.EndOfData)
        {
            rowNumber++;
            var fields = parser.ReadFields();
            if (fields == null || IsCsvRowEmpty(fields))
                continue;

            try
            {
                var nationalId = GetCsvValue(fields, headerMap, "NationalID", "NationalId", "National_Id", "NID");
                var phoneNumber = NormalizePhone(GetCsvValue(fields, headerMap, "PhoneNumber", "Phone Number", "Phone", "Mobile"));
                var machineCode = GetCsvValue(fields, headerMap, "MachineCode", "Machine Code");
                var employeeCode = GetCsvValue(fields, headerMap, "EmployeeCode", "Employee Code");
                var roleText = GetCsvValue(fields, headerMap, "Role");

                var fullName = GetCsvValue(fields, headerMap, "Name", "FullName", "Full Name");
                var (firstName, middleName, lastName, isLatin) = SplitFullName(fullName);
                var latinFirst = !isLatin ? TransliterateArabicToLatin(firstName) : firstName;
                var latinMiddle = !isLatin ? TransliterateArabicToLatin(middleName) : middleName;
                var latinLast = !isLatin ? TransliterateArabicToLatin(lastName) : lastName;

                var departmentName = GetCsvValue(fields, headerMap, "Department", "DepartmentName", "Department Name");
                var jobTitleName = GetCsvValue(fields, headerMap, "JobTitle", "Job Tiltle", "JobTiltle", "Job Title");

                var startDate = ParseDateOnlyLoose(GetCsvValue(fields, headerMap, "HiringDate", "Hiring Date", "StartDate", "Start Date"));
                var religion = ParseEnumLoose<internalEmployee.Auth.Models.Religion>(GetCsvValue(fields, headerMap, "Religion"));
                var allowMobileAttendanceFromAnyLocation = ParseLooseBoolean(GetCsvValue(fields, headerMap, "AllowMobileAttendanceFromAnyLocation", "Allow Mobile Attendance From Any Location"));

                var grossSalary = ParseDecimalLoose(GetCsvValue(fields, headerMap, "GrossSalary", "Gross Salary"));
                var shiftRate = ParseDecimalLoose(GetCsvValue(fields, headerMap, "ShiftRate", "Shift Rate"));
                var bonus = ParseDecimalLoose(GetCsvValue(fields, headerMap, "Bouns", "Bonus"));
                var transportation = ParseDecimalLoose(GetCsvValue(fields, headerMap, "tran", "Transportation", "TransportationAllowance", "Transportation Allowance"));
                var insuranceAllowance = ParseDecimalLoose(GetCsvValue(fields, headerMap, "InsuranceAllowance", "Insurance Allowance"));

                var departmentId = await ResolveOrCreateDepartmentIdAsync(departmentName, departmentMap, ct);
                var jobId = await ResolveOrCreateJobTitleIdAsync(jobTitleName, jobTitleMap, ct);

                var existing = FindExistingUser(
                    nationalId,
                    machineCode,
                    phoneNumber,
                    employeeCode,
                    usersByNationalId,
                    usersByMachineCode,
                    usersByPhone,
                    usersByEmployeeCode);

                if (existing != null)
                {
                    var update = new UpdateEmployeeForm();
                    if (!string.IsNullOrWhiteSpace(nationalId)) update.NationalId = nationalId;
                    if (!string.IsNullOrWhiteSpace(phoneNumber)) update.PhoneNumber = phoneNumber;
                    if (!string.IsNullOrWhiteSpace(employeeCode)) update.EmployeeCode = employeeCode;

                    if (isLatin)
                    {
                        update.FirstNameEn = firstName;
                        update.MiddleNameEn = middleName;
                        update.LastNameEn = lastName;
                    }
                    else
                    {
                        update.FirstNameAr = firstName;
                        update.MiddleNameAr = middleName;
                        update.LastNameAr = lastName;

                        // Only fill English if currently empty to avoid overwriting real English names
                        if (string.IsNullOrWhiteSpace(existing.FirstNameEn)) update.FirstNameEn = latinFirst;
                        if (string.IsNullOrWhiteSpace(existing.MiddleNameEn)) update.MiddleNameEn = latinMiddle;
                        if (string.IsNullOrWhiteSpace(existing.LastNameEn)) update.LastNameEn = latinLast;
                    }

                    update.DepartmentId = departmentId;
                    update.JobId = jobId;
                    update.Religion = religion;
                    update.AllowMobileAttendanceFromAnyLocation = allowMobileAttendanceFromAnyLocation;
                    update.StartDate = startDate;
                    update.GrossSalary = grossSalary;
                    update.ShiftRate = shiftRate;
                    update.MealAllowance = bonus; // CSV "Bouns"
                    update.TransportationAllowance = transportation;
                    update.InsuranceAllowance = insuranceAllowance;
                    update.MachineCode = machineCode;

                    await _authService.UpdateEmployeeAsync(existing.Id, update, ct);
                    updated++;
                }
                else
                {
                    var password = GetCsvValue(fields, headerMap, "Password");
                    if (string.IsNullOrWhiteSpace(password))
                        password = "ChangeMe123!";

                    if (string.IsNullOrWhiteSpace(employeeCode))
                        employeeCode = GenerateUniqueEmployeeCode(usedEmployeeCodes, machineCode, nationalId, phoneNumber, rowNumber);

                    var role = ParseRole(roleText);
                    EnsureRoleAllowedForCaller(role);

                    var form = new AddEmployeeForm
                    {
                        NationalId = nationalId,
                        Password = password ?? string.Empty,
                        PhoneNumber = phoneNumber ?? string.Empty,
                        DepartmentId = departmentId,
                        JobId = jobId,
                        Religion = religion,
                        AllowMobileAttendanceFromAnyLocation = allowMobileAttendanceFromAnyLocation ?? false,
                        StartDate = startDate,
                        GrossSalary = grossSalary,
                        ShiftRate = shiftRate,
                        MealAllowance = bonus,
                        TransportationAllowance = transportation,
                        InsuranceAllowance = insuranceAllowance,
                        MachineCode = machineCode,
                        EmployeeCode = employeeCode,
                        Role = role
                    };

                    if (isLatin)
                    {
                        form.FirstNameEn = firstName;
                        form.MiddleNameEn = middleName;
                        form.LastNameEn = lastName;
                    }
                    else
                    {
                        form.FirstNameAr = firstName;
                        form.MiddleNameAr = middleName;
                        form.LastNameAr = lastName;
                        form.FirstNameEn = latinFirst;
                        form.MiddleNameEn = latinMiddle;
                        form.LastNameEn = latinLast;
                    }

                    if (string.IsNullOrWhiteSpace(form.Password))
                        throw new InvalidOperationException("Password is required.");
                    if (string.IsNullOrWhiteSpace(form.PhoneNumber))
                        throw new InvalidOperationException("PhoneNumber is required.");

                    await _authService.AddEmployeeAsync(form, ct);
                    added++;
                }
            }
            catch (Exception ex)
            {
                var detail = ex.GetBaseException().Message;
                results.Add(new { Row = rowNumber, Error = detail });
            }
        }

        return Ok(new
        {
            Added = added,
            Updated = updated,
            Failed = results.Count,
            Errors = results
        });
    }

    private static string[] GetEmployeeImportHeaders() => new[]
    {
        "NationalId", "Password",
        "FirstNameAr", "MiddleNameAr", "LastNameAr",
        "FirstNameEn", "MiddleNameEn", "LastNameEn",
        "IsMale", "NationalityName", "Religion", "AllowMobileAttendanceFromAnyLocation", "MaritalStatusName", "Birthday",
        "PhoneNumber", "Email", "HomePhone",
        "AddressAr", "AddressEn", "GovernorateName", "CityName",
        "DepartmentName", "BranchName", "JobTitleName", "EmploymentModeName",
        "StartDate", "ContractEndDate",
        "GrossSalary", "ShiftRate", "HousingAllowance", "MealAllowance", "TransportationAllowance", "InsuranceAllowance",
        "OvertimeRate", "InsuranceSalary", "IsInsured", "InsuranceCompanyName",
        "SickLeaveBalance",
        "WorkType", "WorkFromHomeDays", "PartTimeStart", "PartTimeEnd",
        "CompanyEmail", "CompanyPhoneNumber",
        "BankName", "AccountNumber", "IbanNumber", "SwiftBicCode", "BankBranchCode",
        "MachineCode",
        "Role"
    };

    private async Task<int?> ResolveOrCreateDepartmentIdAsync(string? name, Dictionary<string, int> map, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var key = NormalizeKey(name);
        if (map.TryGetValue(key, out var existingId)) return existingId;

        var entity = new internalEmployee.Data.Entities.Department
        {
            Name = name.Trim()
        };
        _db.Departments.Add(entity);
        await _db.SaveChangesAsync(ct);
        map[key] = entity.Id;
        return entity.Id;
    }

    private async Task<int?> ResolveOrCreateJobTitleIdAsync(string? name, Dictionary<string, int> map, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var key = NormalizeKey(name);
        if (map.TryGetValue(key, out var existingId)) return existingId;

        var entity = new internalEmployee.Data.Entities.JobTitle
        {
            Name = name.Trim()
        };
        _db.JobTitles.Add(entity);
        await _db.SaveChangesAsync(ct);
        map[key] = entity.Id;
        return entity.Id;
    }

    private static Dictionary<string, int> BuildNameMap(IEnumerable<(int Id, string Name, string? NameAr)> items)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.Name))
                map[NormalizeKey(item.Name)] = item.Id;
            if (!string.IsNullOrWhiteSpace(item.NameAr))
                map[NormalizeKey(item.NameAr)] = item.Id;
        }
        return map;
    }

    private static Dictionary<string, Guid> BuildUserFullNameMap(IEnumerable<internalEmployee.Auth.Models.AppUser> users)
    {
        var map = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var u in users)
        {
            var fullNameAr = string.Join(" ", new[] { u.FirstNameAr, u.MiddleNameAr, u.LastNameAr }
                .Where(n => !string.IsNullOrWhiteSpace(n)));
            var fullNameEn = string.Join(" ", new[] { u.FirstNameEn, u.MiddleNameEn, u.LastNameEn }
                .Where(n => !string.IsNullOrWhiteSpace(n)));

            if (!string.IsNullOrWhiteSpace(fullNameAr))
                map[NormalizeKey(fullNameAr)] = u.Id;
            if (!string.IsNullOrWhiteSpace(fullNameEn))
                map[NormalizeKey(fullNameEn)] = u.Id;
        }
        return map;
    }

    private static int? ResolveId(string? name, Dictionary<string, int> map, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var key = NormalizeKey(name);
        if (map.TryGetValue(key, out var id)) return id;
        throw new InvalidOperationException($"{fieldName} not found: {name}");
    }

    private static string? ResolveUserId(string? name, Dictionary<string, Guid> map, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var key = NormalizeKey(name);
        if (map.TryGetValue(key, out var id)) return id.ToString();
        throw new InvalidOperationException($"{fieldName} not found: {name}");
    }

    private static string NormalizeKey(string value)
        => value.Trim().ToLowerInvariant();

    private static Dictionary<string, internalEmployee.Auth.Models.AppUser> BuildUserIndex(
        IEnumerable<internalEmployee.Auth.Models.AppUser> users,
        Func<internalEmployee.Auth.Models.AppUser, string?> selector)
    {
        var map = new Dictionary<string, internalEmployee.Auth.Models.AppUser>(StringComparer.OrdinalIgnoreCase);
        foreach (var u in users)
        {
            var key = selector(u)?.Trim();
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (!map.ContainsKey(key))
                map[key] = u;
        }
        return map;
    }

    private static internalEmployee.Auth.Models.AppUser? FindExistingUser(
        string? nationalId,
        string? machineCode,
        string? phoneNumber,
        string? employeeCode,
        Dictionary<string, internalEmployee.Auth.Models.AppUser> byNationalId,
        Dictionary<string, internalEmployee.Auth.Models.AppUser> byMachineCode,
        Dictionary<string, internalEmployee.Auth.Models.AppUser> byPhone,
        Dictionary<string, internalEmployee.Auth.Models.AppUser> byEmployeeCode)
    {
        if (!string.IsNullOrWhiteSpace(nationalId) && byNationalId.TryGetValue(nationalId.Trim(), out var u1))
            return u1;
        if (!string.IsNullOrWhiteSpace(machineCode) && byMachineCode.TryGetValue(machineCode.Trim(), out var u2))
            return u2;
        if (!string.IsNullOrWhiteSpace(phoneNumber) && byPhone.TryGetValue(phoneNumber.Trim(), out var u3))
            return u3;
        if (!string.IsNullOrWhiteSpace(employeeCode) && byEmployeeCode.TryGetValue(employeeCode.Trim(), out var u4))
            return u4;
        return null;
    }

    private static Dictionary<string, int> BuildNormalizedHeaderMap(string[] headers)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++)
        {
            var key = NormalizeHeaderKey(headers[i]);
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (!map.ContainsKey(key))
                map[key] = i;
        }
        return map;
    }

    private static string NormalizeHeaderKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
                sb.Append(ch);
        }
        return sb.ToString();
    }

    private static string? GetCsvValue(string[] fields, Dictionary<string, int> headerMap, params string[] headerKeys)
    {
        foreach (var key in headerKeys)
        {
            var normalized = NormalizeHeaderKey(key);
            if (headerMap.TryGetValue(normalized, out var idx) && idx >= 0 && idx < fields.Length)
                return fields[idx]?.Trim();
        }
        return null;
    }

    private static bool IsCsvRowEmpty(string[] fields)
    {
        foreach (var f in fields)
        {
            if (!string.IsNullOrWhiteSpace(f))
                return false;
        }
        return true;
    }

    private static decimal? ParseDecimalLoose(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var sb = new StringBuilder();
        foreach (var ch in text)
        {
            if (char.IsDigit(ch) || ch == '.' || ch == ',' || ch == '-')
                sb.Append(ch);
        }
        var cleaned = sb.ToString();
        if (string.IsNullOrWhiteSpace(cleaned)) return null;

        var invariant = cleaned.Replace(",", ".");
        if (decimal.TryParse(invariant, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var val))
            return val;
        if (decimal.TryParse(cleaned, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.CurrentCulture, out var val2))
            return val2;
        return null;
    }

    private static DateOnly? ParseDateOnlyLoose(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1)) return d1;
        if (DateOnly.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var d2)) return d2;
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt1)) return DateOnly.FromDateTime(dt1);
        if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dt2)) return DateOnly.FromDateTime(dt2);
        return null;
    }

    private static bool? ParseLooseBoolean(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        text = text.Trim();
        if (bool.TryParse(text, out var value)) return value;
        if (text == "1") return true;
        if (text == "0") return false;
        if (text.Equals("yes", StringComparison.OrdinalIgnoreCase)) return true;
        if (text.Equals("no", StringComparison.OrdinalIgnoreCase)) return false;
        if (text.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
        if (text.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
        return null;
    }

    private static (string? First, string? Middle, string? Last, bool IsLatin) SplitFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return (null, null, null, false);

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 1)
            return (parts[0], null, null, HasLatinLetters(parts[0]));
        if (parts.Length == 2)
            return (parts[0], null, parts[1], HasLatinLetters(fullName));

        var first = parts[0];
        var last = parts[^1];
        var middle = string.Join(" ", parts.Skip(1).SkipLast(1));
        return (first, middle, last, HasLatinLetters(fullName));
    }

    private static bool HasLatinLetters(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        foreach (var ch in text)
        {
            if (ch <= 127 && char.IsLetter(ch))
                return true;
        }
        return false;
    }

    private static Encoding DetectCsvEncoding(byte[] bytes)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        // UTF-8 BOM
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return new UTF8Encoding(true);

        // Try UTF-8 and see if we get replacement chars
        var utf8 = Encoding.UTF8;
        var text = utf8.GetString(bytes);
        if (text.Contains('\uFFFD'))
            return Encoding.GetEncoding(1256); // Windows-1256 (Arabic)

        return utf8;
    }

    private static string? TransliterateArabicToLatin(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        var sb = new StringBuilder(input.Length * 2);
        foreach (var ch in input)
        {
            sb.Append(ch switch
            {
                'ا' or 'أ' or 'إ' or 'آ' => "a",
                'ب' => "b",
                'ت' => "t",
                'ث' => "th",
                'ج' => "j",
                'ح' => "h",
                'خ' => "kh",
                'د' => "d",
                'ذ' => "dh",
                'ر' => "r",
                'ز' => "z",
                'س' => "s",
                'ش' => "sh",
                'ص' => "s",
                'ض' => "d",
                'ط' => "t",
                'ظ' => "z",
                'ع' => "a",
                'غ' => "gh",
                'ف' => "f",
                'ق' => "q",
                'ك' => "k",
                'ل' => "l",
                'م' => "m",
                'ن' => "n",
                'ه' => "h",
                'و' => "w",
                'ي' or 'ى' => "y",
                'ة' => "a",
                'ء' => "",
                _ => ch.ToString()
            });
        }
        return sb.ToString();
    }

    private static string GenerateUniqueEmployeeCode(
        HashSet<string> usedEmployeeCodes,
        string? machineCode,
        string? nationalId,
        string? phoneNumber,
        int rowNumber)
    {
        string? baseCode = null;
        if (!string.IsNullOrWhiteSpace(machineCode)) baseCode = machineCode.Trim();
        else if (!string.IsNullOrWhiteSpace(nationalId)) baseCode = nationalId.Trim();
        else if (!string.IsNullOrWhiteSpace(phoneNumber)) baseCode = phoneNumber.Trim();

        if (string.IsNullOrWhiteSpace(baseCode))
            baseCode = $"IMP{DateTime.Now:yyyyMMddHHmmssfff}{rowNumber:D4}";

        var candidate = baseCode;
        var suffix = 1;
        while (usedEmployeeCodes.Contains(candidate))
        {
            candidate = $"{baseCode}-{suffix++}";
        }

        usedEmployeeCodes.Add(candidate);
        return candidate;
    }

    private static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return phone;
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits)) return phone?.Trim();
        if (digits.Length == 10 && !digits.StartsWith("0", StringComparison.Ordinal))
            return "0" + digits;
        return digits;
    }

    private static internalEmployee.Auth.Models.AppRole? ParseRole(string? roleText)
    {
        if (string.IsNullOrWhiteSpace(roleText)) return null;
        if (Enum.TryParse<internalEmployee.Auth.Models.AppRole>(roleText.Trim(), true, out var role))
            return role;
        throw new InvalidOperationException($"Invalid Role: {roleText}");
    }

    private void EnsureRoleAllowedForCaller(internalEmployee.Auth.Models.AppRole? role)
    {
        if (!role.HasValue) return;
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var isAdmin = User.IsInRole("Admin");

        // Only SuperAdmin can assign Admin or SuperAdmin
        if ((role == internalEmployee.Auth.Models.AppRole.Admin || role == internalEmployee.Auth.Models.AppRole.SuperAdmin) && !isSuperAdmin)
            throw new InvalidOperationException("Not allowed to assign Admin/SuperAdmin role.");

        // Admin or SuperAdmin can assign HR
        // HR can assign HR/User as per roles used in the system
        var isHr = User.IsInRole("HR");
        if (role == internalEmployee.Auth.Models.AppRole.HR && !isSuperAdmin && !isAdmin && !isHr)
            throw new InvalidOperationException("Not allowed to assign HR role.");
    }

    private static void WriteListColumn(ExcelWorksheet ws, int column, string header, IEnumerable<string?> values)
    {
        ws.Cells[1, column].Value = header;
        int row = 2;
        foreach (var v in values)
        {
            if (string.IsNullOrWhiteSpace(v)) continue;
            ws.Cells[row++, column].Value = v;
        }
    }

    private static void AddListValidation(
        ExcelWorksheet targetSheet,
        string headerName,
        ExcelWorksheet listsSheet,
        string listHeaderName,
        int startRow,
        int endRow)
    {
        var headerMap = BuildHeaderMap(targetSheet);
        if (!headerMap.TryGetValue(headerName, out var targetCol))
            return;

        var listHeaderMap = BuildHeaderMap(listsSheet);
        if (!listHeaderMap.TryGetValue(listHeaderName, out var listCol))
            return;

        var lastRow = listsSheet.Dimension?.End.Row ?? 1;
        if (lastRow < 2) return;

        var listRange = $"{listsSheet.Name}!${GetColumnLetter(listCol)}$2:${GetColumnLetter(listCol)}${lastRow}";
        var validation = targetSheet.DataValidations.AddListValidation(
            targetSheet.Cells[startRow, targetCol, endRow, targetCol].Address);
        validation.Formula.ExcelFormula = listRange;
        validation.ShowErrorMessage = true;
    }

    private static string GetColumnLetter(int columnNumber)
    {
        var dividend = columnNumber;
        var columnName = string.Empty;
        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar(65 + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }
        return columnName;
    }

    private static Dictionary<string, int> BuildHeaderMap(ExcelWorksheet ws)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastCol = ws.Dimension.End.Column;
        for (int col = 1; col <= lastCol; col++)
        {
            var header = ws.Cells[1, col].Text?.Trim();
            if (!string.IsNullOrWhiteSpace(header) && !map.ContainsKey(header))
            {
                map[header] = col;
            }
        }
        return map;
    }

    private static bool IsRowEmpty(ExcelWorksheet ws, int row, IEnumerable<int> cols)
    {
        foreach (var col in cols)
        {
            if (!string.IsNullOrWhiteSpace(ws.Cells[row, col].Text))
                return false;
        }
        return true;
    }

    private static string? GetString(ExcelWorksheet ws, int row, Dictionary<string, int> map, string key)
        => map.TryGetValue(key, out var col) ? ws.Cells[row, col].Text?.Trim() : null;

    private static int? GetInt(ExcelWorksheet ws, int row, Dictionary<string, int> map, string key)
    {
        if (!map.TryGetValue(key, out var col)) return null;
        if (int.TryParse(ws.Cells[row, col].Text?.Trim(), out var val)) return val;
        return null;
    }

    private static decimal? GetDecimal(ExcelWorksheet ws, int row, Dictionary<string, int> map, string key)
    {
        if (!map.TryGetValue(key, out var col)) return null;
        if (decimal.TryParse(ws.Cells[row, col].Text?.Trim(), out var val)) return val;
        return null;
    }

    private static bool? GetBool(ExcelWorksheet ws, int row, Dictionary<string, int> map, string key)
    {
        if (!map.TryGetValue(key, out var col)) return null;
        var text = ws.Cells[row, col].Text?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (bool.TryParse(text, out var val)) return val;
        if (text == "1") return true;
        if (text == "0") return false;
        if (text.Equals("yes", StringComparison.OrdinalIgnoreCase)) return true;
        if (text.Equals("no", StringComparison.OrdinalIgnoreCase)) return false;
        if (text.Equals("male", StringComparison.OrdinalIgnoreCase)) return true;
        if (text.Equals("female", StringComparison.OrdinalIgnoreCase)) return false;
        if (text.Equals("ذكر", StringComparison.OrdinalIgnoreCase)) return true;
        if (text.Equals("أنثى", StringComparison.OrdinalIgnoreCase) || text.Equals("انثى", StringComparison.OrdinalIgnoreCase)) return false;
        return null;
    }

    private static DateOnly? GetDateOnly(ExcelWorksheet ws, int row, Dictionary<string, int> map, string key)
    {
        if (!map.TryGetValue(key, out var col)) return null;
        var cell = ws.Cells[row, col].Value;
        if (cell is DateTime dt) return DateOnly.FromDateTime(dt);
        if (DateOnly.TryParse(ws.Cells[row, col].Text?.Trim(), out var d)) return d;
        if (DateTime.TryParse(ws.Cells[row, col].Text?.Trim(), out var dt2)) return DateOnly.FromDateTime(dt2);
        return null;
    }

    private static TimeOnly? GetTimeOnly(ExcelWorksheet ws, int row, Dictionary<string, int> map, string key)
    {
        if (!map.TryGetValue(key, out var col)) return null;
        var cell = ws.Cells[row, col].Value;
        if (cell is TimeSpan ts) return TimeOnly.FromTimeSpan(ts);
        if (cell is DateTime dt) return TimeOnly.FromDateTime(dt);
        if (TimeOnly.TryParse(ws.Cells[row, col].Text?.Trim(), out var t)) return t;
        if (TimeSpan.TryParse(ws.Cells[row, col].Text?.Trim(), out var ts2)) return TimeOnly.FromTimeSpan(ts2);
        return null;
    }

    private static List<DayOfWeek>? GetDaysOfWeek(ExcelWorksheet ws, int row, Dictionary<string, int> map, string key)
    {
        if (!map.TryGetValue(key, out var col)) return null;
        var text = ws.Cells[row, col].Text?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return null;

        var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var days = new List<DayOfWeek>();
        foreach (var part in parts)
        {
            if (int.TryParse(part, out var d) && d >= 0 && d <= 6)
            {
                days.Add((DayOfWeek)d);
                continue;
            }
            if (Enum.TryParse<DayOfWeek>(part, true, out var day))
            {
                days.Add(day);
                continue;
            }
        }
        return days.Count == 0 ? null : days;
    }

    private static TEnum GetEnumOrDefault<TEnum>(TEnum defaultValue, ExcelWorksheet ws, int row, Dictionary<string, int> map, string key)
        where TEnum : struct
    {
        if (!map.TryGetValue(key, out var col)) return defaultValue;
        var text = ws.Cells[row, col].Text?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return defaultValue;
        if (Enum.TryParse<TEnum>(text, true, out var val)) return val;
        if (int.TryParse(text, out var intVal) && Enum.IsDefined(typeof(TEnum), intVal))
            return (TEnum)Enum.ToObject(typeof(TEnum), intVal);
        return defaultValue;
    }

    private static TEnum? GetEnumNullable<TEnum>(ExcelWorksheet ws, int row, Dictionary<string, int> map, string key)
        where TEnum : struct
    {
        if (!map.TryGetValue(key, out var col)) return null;
        var text = ws.Cells[row, col].Text?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (Enum.TryParse<TEnum>(text, true, out var val)) return val;
        if (int.TryParse(text, out var intVal) && Enum.IsDefined(typeof(TEnum), intVal))
            return (TEnum)Enum.ToObject(typeof(TEnum), intVal);
        return null;
    }

    private static TEnum? ParseEnumLoose<TEnum>(string? text)
        where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        text = text.Trim();
        if (Enum.TryParse<TEnum>(text, true, out var val)) return val;
        if (int.TryParse(text, out var intVal) && Enum.IsDefined(typeof(TEnum), intVal))
            return (TEnum)Enum.ToObject(typeof(TEnum), intVal);
        return null;
    }
}
