using internalEmployee.Auth.Contracts;
using internalEmployee.Services.Home;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class HomeController : ControllerBase
{
    private readonly IHomeService _homeService;

    public HomeController(IHomeService homeService)
    {
        _homeService = homeService;
    }



    [HttpGet]
    [ProducesResponseType(typeof(HomeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HomeResponse>> GetHome(CancellationToken ct)
    {
        try
        {
            var result = await _homeService.GetHomeAsync(User, ct);
            var response = new HomeResponse
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
                RejectedRequests = result.RejectedRequests
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

    [HttpGet("viewall")]
    [ProducesResponseType(typeof(List<RequestItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<RequestItem>>> ViewAll([FromQuery] int? month, CancellationToken ct)
    {
        try
        {
            var requests = await _homeService.GetAllRequestsAsync(User, month, ct);
            return Ok(requests);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpGet("viewallpending")]
    [ProducesResponseType(typeof(List<RequestItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<RequestItem>>> ViewAllPending([FromQuery] int? month, CancellationToken ct)
    {
        try
        {
            var requests = await _homeService.GetAllPendingRequestsAsync(User, month, ct);
            return Ok(requests);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpGet("viewallaccepted")]
    [ProducesResponseType(typeof(List<RequestItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<RequestItem>>> ViewAllAccepted([FromQuery] int? month, CancellationToken ct)
    {
        try
        {
            var requests = await _homeService.GetAllAcceptedRequestsAsync(User, month, ct);
            return Ok(requests);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpGet("viewallrejected")]
    [ProducesResponseType(typeof(List<RequestItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<RequestItem>>> ViewAllRejected([FromQuery] int? month, CancellationToken ct)
    {
        try
        {
            var requests = await _homeService.GetAllRejectedRequestsAsync(User, month, ct);
            return Ok(requests);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpGet("hr")]
    [Authorize(Roles = "HR")]
    [ProducesResponseType(typeof(HRHomeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HRHomeResponse>> GetHRHome(CancellationToken ct)
    {
        try
        {
            var result = await _homeService.GetHRHomeAsync(User, ct);
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

            var response = new HRHomeResponse
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
                Employees = mappedEmployees,
                Statistics = result.Statistics
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

