using internalEmployee.Auth.Contracts;
using internalEmployee.Auth.Models;

namespace internalEmployee.Services.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<AppUser?> GetUserByNationalIdAsync(string nationalId, CancellationToken ct = default);
    AppUser? GetUserByNationalId(string nationalId); // Synchronous for backward compatibility
    Task<List<AppUser>> GetAllUsersAsync(CancellationToken ct);
    Task<AppUser?> GetUserByIdAsync(Guid userId, CancellationToken ct);
    Task<PaginatedResponse<UserResponse>> GetAllUsersWithDetailsAsync(int pageNumber, int pageSize, string? searchQuery, int? departmentId, int? branchId, int? jobId, bool? isActive, CancellationToken ct);
    Task<UserResponse?> GetUserWithDetailsAsync(Guid userId, CancellationToken ct);
    Task<UserProfileResponse?> GetUserProfileWithDetailsAsync(Guid userId, CancellationToken ct);
    Task<EmployeeDetailsResponse?> GetEmployeeByIdWithDetailsAsync(Guid employeeId, CancellationToken ct);
    Task<List<EmployeeDetailsResponse>> GetAllEmployeesWithDetailsAsync(CancellationToken ct);
    Task<AppUser> UpdateUserProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct);
    Task<bool> SendForgotPasswordOtpAsync(ForgotPasswordRequest request, CancellationToken ct);
    Task VerifyPasswordResetOtpAsync(VerifyResetOtpRequest request, CancellationToken ct);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct);
    Task<List<AppUser>> GetAllAdminsAsync(CancellationToken ct);
    Task<List<AppUser>> GetAdminsByDepartmentIdAsync(int departmentId, CancellationToken ct);
    Task<AppUser> CreateAdminAsync(CreateAdminRequest request, CancellationToken ct);
    Task UpdateAdminPasswordAsync(UpdateAdminPasswordRequest request, CancellationToken ct);
    Task<OrganizationChartResponse> GetOrganizationChartAsync(CancellationToken ct);
    Task UpdateUserRoleAsync(Guid userId, UpdateUserRoleRequest request, CancellationToken ct);
    Task UpdateUserRoleByHRAsync(Guid userId, UpdateUserRoleRequest request, CancellationToken ct);
    Task<AuthResponse> AddEmployeeAsync(AddEmployeeForm request, CancellationToken ct);
    Task<EmployeeDetailsResponse> UpdateEmployeeAsync(Guid employeeId, UpdateEmployeeForm request, CancellationToken ct);
    Task ChangeEmployeeActiveStatusAsync(Guid employeeId, bool isActive, CancellationToken ct);
    Task DeleteEmployeeAsync(Guid employeeId, CancellationToken ct);
    Task DeleteEmployeeImageAsync(Guid employeeId, CancellationToken ct);
    Task ChangeSalaryAsync(Guid employeeId, ChangeSalaryRequest request, CancellationToken ct);
    Task TransferDepartmentAsync(Guid employeeId, TransferDepartmentRequest request, CancellationToken ct);
    Task ChangeJobTitleAsync(Guid employeeId, ChangeJobTitleRequest request, CancellationToken ct);
    Task UpdateAllowancesAsync(Guid employeeId, UpdateAllowancesRequest request, CancellationToken ct);
}
