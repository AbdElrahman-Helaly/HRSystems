using internalEmployee.Auth.Contracts;
using internalEmployee.Services.SuperAdminDashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public sealed class SuperAdminDashboardController : ControllerBase
{
    private readonly ISuperAdminDashboardService _superAdminDashboardService;

    public SuperAdminDashboardController(ISuperAdminDashboardService superAdminDashboardService)
    {
        _superAdminDashboardService = superAdminDashboardService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(SuperAdminDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SuperAdminDashboardResponse>> GetSuperAdminDashboard(CancellationToken ct)
    {
        try
        {
            var result = await _superAdminDashboardService.GetSuperAdminDashboardAsync(User, ct);

            var mappedDepartments = result.Departments.Select(d => new DepartmentDashboardItem
            {
                DepartmentId = d.DepartmentId,
                DepartmentName = d.DepartmentName,
                Employees = d.Employees.Select(e => new AdminUserItem
                {
                    Id = e.Id,
                    FirstNameAr = e.FirstNameAr,
                    MiddleNameAr = e.MiddleNameAr,
                    LastNameAr = e.LastNameAr,
                    FirstNameEn = e.FirstNameEn,
                    MiddleNameEn = e.MiddleNameEn,
                    LastNameEn = e.LastNameEn,
                    FullNameAr = e.FullNameAr,
                    FullNameEn = e.FullNameEn,
                    IsActive = e.IsActive,
                    ImageUrl = GetImageUrl(e.ImageUrl),
                    TodayAttendanceTime = e.TodayAttendanceTime,
                    TodayDepartureTime = e.TodayDepartureTime
                }).ToList(),
                Requests = d.Requests
            }).ToList();

            var response = new SuperAdminDashboardResponse
            {
                Greeting = result.Greeting,
                FullNameAr = result.FullNameAr,
                FullNameEn = result.FullNameEn,
                JobTitle = result.JobTitle,
                ImageUrl = GetImageUrl(result.ImageUrl),
                TodayAttendanceTime = result.TodayAttendanceTime,
                TodayDepartureTime = result.TodayDepartureTime,
                Departments = mappedDepartments
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
}
