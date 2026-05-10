using internalEmployee.Auth.Contracts;
using internalEmployee.Services.AdminDashboard;
using internalEmployee.Services.Assignment;
using internalEmployee.Services.Leave;
using internalEmployee.Services.Permission;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using RequestStatus = internalEmployee.Data.Entities.RequestStatus;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _adminDashboardService;
    private readonly IPermissionService _permissionService;
    private readonly IAssignmentService _assignmentService;
    private readonly ILeaveService _leaveService;

    public AdminDashboardController(
        IAdminDashboardService adminDashboardService,
        IPermissionService permissionService,
        IAssignmentService assignmentService,
        ILeaveService leaveService)
    {
        _adminDashboardService = adminDashboardService;
        _permissionService = permissionService;
        _assignmentService = assignmentService;
        _leaveService = leaveService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminDashboardResponse>> GetAdminDashboard(CancellationToken ct)
    {
        try
        {
            var result = await _adminDashboardService.GetAdminDashboardAsync(User, ct);
            var mappedEmployees = result.Employees.Select(e => new EmployeeItem
            {
                Id = e.Id,
                NationalId = e.NationalId,
                PassportNumber = e.PassportNumber,
                FirstNameAr = e.FirstNameAr,
                MiddleNameAr = e.MiddleNameAr,
                LastNameAr = e.LastNameAr,
                FirstNameEn = e.FirstNameEn,
                MiddleNameEn = e.MiddleNameEn,
                LastNameEn = e.LastNameEn,
                Email = e.Email,
                PhoneNumber = e.PhoneNumber,
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
                DepartmentId = e.DepartmentId,
                DepartmentName = e.DepartmentName,
                JobTitle = e.JobTitle,
                Role = e.Role,
                IsPending = e.IsPending,
                IsMale = e.IsMale,
                StartDate = e.StartDate,
                ImageUrl = GetImageUrl(e.ImageUrl),
                NationalityId = e.NationalityId,
                NationalityName = e.NationalityName
            }).ToList();

            var response = new AdminDashboardResponse
            {
                Greeting = result.Greeting,
                FullNameAr = result.FullNameAr,
                FullNameEn = result.FullNameEn,
                JobTitle = result.JobTitle,
                DepartmentName = result.DepartmentName,
                ImageUrl = GetImageUrl(result.ImageUrl),
                TodayAttendanceTime = result.TodayAttendanceTime,
                TodayDepartureTime = result.TodayDepartureTime,
                AllRequests = result.AllRequests,
                PendingRequests = result.PendingRequests,
                AcceptedRequests = result.AcceptedRequests,
                RejectedRequests = result.RejectedRequests,
                Employees = mappedEmployees
            };

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
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

        var request = HttpContext.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}";

        if (!relativeUrl.StartsWith("/"))
            relativeUrl = "/" + relativeUrl;

        return baseUrl + relativeUrl;
    }

    [HttpGet("department-permissions")]
    [ProducesResponseType(typeof(PaginatedResponse<PermissionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResponse<PermissionResponse>>> GetDepartmentPermissions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        CancellationToken ct = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
            return Unauthorized();

        RequestStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<RequestStatus>(status, ignoreCase: true, out var parsed))
        {
            parsedStatus = parsed;
        }

        var result = await _permissionService.GetDepartmentPermissionsPaginatedAsync(
            currentUserId, pageNumber, pageSize, search, parsedStatus, userId, dateFrom, dateTo, ct);

        return Ok(result);
    }

    [HttpGet("department-assignments")]
    [ProducesResponseType(typeof(PaginatedResponse<AssignmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResponse<AssignmentResponse>>> GetDepartmentAssignments(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        CancellationToken ct = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
            return Unauthorized();

        RequestStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<RequestStatus>(status, ignoreCase: true, out var parsed))
        {
            parsedStatus = parsed;
        }

        var result = await _assignmentService.GetDepartmentAssignmentsPaginatedAsync(
            currentUserId, pageNumber, pageSize, search, parsedStatus, userId, dateFrom, dateTo, ct);

        return Ok(result);
    }

    [HttpGet("department-leaves")]
    [ProducesResponseType(typeof(PaginatedResponse<LeaveResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResponse<LeaveResponse>>> GetDepartmentLeaves(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        CancellationToken ct = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
            return Unauthorized();

        RequestStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<RequestStatus>(status, ignoreCase: true, out var parsed))
        {
            parsedStatus = parsed;
        }

        var result = await _leaveService.GetDepartmentLeavesPaginatedAsync(
            currentUserId, pageNumber, pageSize, search, parsedStatus, userId, dateFrom, dateTo, ct);

        return Ok(result);
    }
}

