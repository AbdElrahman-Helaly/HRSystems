using internalEmployee.Auth.Contracts;
using internalEmployee.Auth.Models;
using internalEmployee.Data;
using internalEmployee.Services.Attendance;
using internalEmployee.Services.ZKTeco;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Security.Claims;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly IZKTecoService _zkTecoService;
    private readonly AppDbContext _db;

    public AttendanceController(IAttendanceService attendanceService, IZKTecoService zkTecoService, AppDbContext db)
    {
        _attendanceService = attendanceService;
        _zkTecoService = zkTecoService;
        _db = db;
    }

 


    [HttpGet("my/{date}")]
    [ProducesResponseType(typeof(AttendanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendanceResponse>> GetMyAttendanceByDate(DateOnly date, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var attendance = await _attendanceService.GetAttendanceByDateAsync(userId, date, ct);
        if (attendance == null)
            return NotFound();

        var response = new AttendanceResponse
        {
            Id = attendance.Id,
            UserId = attendance.UserId,
            Date = attendance.Date,
            AttendanceTime = attendance.AttendanceTime,
            DepartureTime = attendance.DepartureTime,
            DeviceType = attendance.DeviceType,
            Location = attendance.Location,
            LocationId = attendance.LocationId,
            CreatedAt = attendance.CreatedAt,
            UpdatedAt = attendance.UpdatedAt
        };
        return Ok(response);
    }

    

    [HttpGet("attendance")]
    [ProducesResponseType(typeof(List<AttendanceItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<AttendanceItemResponse>>> GetMyAttendanceRecords([FromQuery] int? month, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var attendances = await _attendanceService.GetUserAttendanceRecordsAsync(userId, month, ct);
        var responses = attendances
            .Where(a => a.AttendanceTime.HasValue)
            .Select(a => new AttendanceItemResponse
            {
                Id = a.Id,
                Date = a.Date,
                AttendanceTime = a.AttendanceTime!.Value,
                CreatedAt = a.CreatedAt
            }).ToList();
        return Ok(responses);
    }

    [HttpGet("departure")]
    [ProducesResponseType(typeof(List<DepartureItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<DepartureItemResponse>>> GetMyDepartureRecords([FromQuery] int? month, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var departures = await _attendanceService.GetUserDepartureRecordsAsync(userId, month, ct);
        var responses = departures
            .Where(a => a.DepartureTime.HasValue)
            .Select(a => new DepartureItemResponse
            {
                Id = a.Id,
                Date = a.Date,
                DepartureTime = a.DepartureTime!.Value,
                CreatedAt = a.CreatedAt
            }).ToList();
        return Ok(responses);
    }





    // Mobile endpoints - JWT required (userId extracted from token)
    [HttpPost("mobile/checkin")]
    [Authorize]
    [ProducesResponseType(typeof(AttendanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AttendanceResponse>> MobileCheckIn([FromBody] MobileAttendanceRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var attendance = await _attendanceService.CreateMobileAttendanceAsync(userId, request, ct);
            var response = new AttendanceResponse
            {
                Id = attendance.Id,
                UserId = attendance.UserId,
                Date = attendance.Date,
                AttendanceTime = attendance.AttendanceTime,
                DepartureTime = attendance.DepartureTime,
                DeviceType = attendance.DeviceType,
                Location = attendance.Location,
                LocationId = attendance.LocationId,
                CreatedAt = attendance.CreatedAt,
                UpdatedAt = attendance.UpdatedAt
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

    [HttpPost("mobile/checkout")]
    [Authorize]
    [ProducesResponseType(typeof(AttendanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AttendanceResponse>> MobileCheckOut([FromBody] MobileDepartureRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var attendance = await _attendanceService.CreateMobileDepartureAsync(userId, request, ct);
            var response = new AttendanceResponse
            {
                Id = attendance.Id,
                UserId = attendance.UserId,
                Date = attendance.Date,
                AttendanceTime = attendance.AttendanceTime,
                DepartureTime = attendance.DepartureTime,
                DeviceType = attendance.DeviceType,
                Location = attendance.Location,
                LocationId = attendance.LocationId,
                CreatedAt = attendance.CreatedAt,
                UpdatedAt = attendance.UpdatedAt
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

    // ZKTeco Device endpoints - Fetch logs from device
    [HttpPost("device/fetch-logs")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(List<DeviceAttendanceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<DeviceAttendanceResponse>>> FetchDeviceLogs([FromBody] FetchDeviceLogsRequest request, CancellationToken ct)
    {
        try
        {
            // Fixed device connection settings
            const string deviceIpAddress = "192.168.1.152";
            const int devicePort = 4370;
            const int machineNumber = 1;

            // Connect to device
            var connected = await _zkTecoService.ConnectAsync(deviceIpAddress, devicePort, machineNumber);
            if (!connected)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "فشل الاتصال بجهاز البصمة",
                    detail: $"لا يمكن الاتصال بالجهاز على العنوان {deviceIpAddress}:{devicePort}");
            }

            try
            {
                // Fetch logs from device
                var logs = await _zkTecoService.GetLogsAsync(request.StartDate, request.EndDate);

                // Apply filter if specified
                if (request.IsCheckInOnly.HasValue)
                {
                    logs = logs.Where(log => log.IsCheckIn == request.IsCheckInOnly.Value).ToList();
                }

                // Group logs by MachineCode and Date, then combine check-in and check-out times
                var groupedLogs = logs
                    .GroupBy(log => new { log.MachineCode, log.Date })
                    .Select(group => new DeviceAttendanceResponse
                    {
                        MachineCode = group.Key.MachineCode,
                        Date = group.Key.Date,
                        CheckInTime = group.Where(log => log.IsCheckIn).OrderBy(log => log.Time).FirstOrDefault()?.Time,
                        CheckOutTime = group.Where(log => !log.IsCheckIn).OrderBy(log => log.Time).LastOrDefault()?.Time,
                        Location = group.FirstOrDefault()?.Location
                    })
                    .OrderBy(r => r.Date)
                    .ThenBy(r => r.MachineCode)
                    .ToList();

                return Ok(groupedLogs);
            }
            finally
            {
                // Always disconnect
                await _zkTecoService.DisconnectAsync();
            }
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




    [HttpPost("device/sync")]
    [ProducesResponseType(typeof(DeviceSyncResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeviceSyncResponse>> SyncDeviceAttendance([FromBody] DeviceSyncRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _attendanceService.SyncDeviceAttendanceAsync(request, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: ex.Message);
        }
    }

    [HttpPost("device/sync-batch")]
    [ProducesResponseType(typeof(DeviceBatchSyncResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeviceBatchSyncResponse>> SyncDeviceAttendanceBatch([FromBody] DeviceBatchSyncRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _attendanceService.SyncDeviceAttendanceBatchAsync(request, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: ex.Message);
        }
    }

    [HttpPost("device/sync-incremental")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(DeviceIncrementalSyncResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeviceIncrementalSyncResponse>> SyncDeviceAttendanceIncremental(
        [FromBody] DeviceIncrementalSyncRequest request,
        CancellationToken ct)
    {
        try
        {
            const string deviceKey = "ZKTeco";

            var state = await _db.DeviceSyncStates.FirstOrDefaultAsync(s => s.DeviceKey == deviceKey, ct);
            DateOnly? lastDate = state?.LastSyncedDate;

            if (lastDate == null && !request.StartDate.HasValue)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "StartDate is required for the first sync.");
            }

            var startDate = lastDate.HasValue ? lastDate.Value.AddDays(1) : request.StartDate!.Value;
            var endDate = request.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            if (startDate > endDate)
            {
                return Ok(new DeviceIncrementalSyncResponse
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalDeviceLogs = 0,
                    SavedCount = 0,
                    Message = "No new dates to sync."
                });
            }

            var logs = await _zkTecoService.GetLogsAsync(startDate, endDate);

            if (request.IsCheckInOnly.HasValue)
            {
                logs = logs.Where(l => l.IsCheckIn == request.IsCheckInOnly.Value).ToList();
            }

            var groupedLogs = logs
                .GroupBy(log => new { log.MachineCode, log.Date })
                .Select(group => new DeviceAttendanceResponse
                {
                    MachineCode = group.Key.MachineCode,
                    Date = group.Key.Date,
                    CheckInTime = group.Where(log => log.IsCheckIn).OrderBy(log => log.Time).FirstOrDefault()?.Time,
                    CheckOutTime = group.Where(log => !log.IsCheckIn).OrderBy(log => log.Time).LastOrDefault()?.Time,
                    Location = group.FirstOrDefault()?.Location
                })
                .OrderBy(r => r.Date)
                .ThenBy(r => r.MachineCode)
                .ToList();

            var savedCount = await _attendanceService.SaveDeviceAttendanceLogsAsync(groupedLogs, ct);

            if (state == null)
            {
                state = new internalEmployee.Data.Entities.DeviceSyncState
                {
                    DeviceKey = deviceKey,
                    LastSyncedDate = endDate,
                    LastSyncedAt = DateTime.UtcNow
                };
                _db.DeviceSyncStates.Add(state);
            }
            else
            {
                state.LastSyncedDate = endDate;
                state.LastSyncedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);

            return Ok(new DeviceIncrementalSyncResponse
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalDeviceLogs = groupedLogs.Count,
                SavedCount = savedCount,
                Message = "Sync completed."
            });
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

    [HttpPut("manual")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(AttendanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AttendanceResponse>> UpdateManualAttendance(
        [FromBody] ManualAttendanceRequest request,
        CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
            return Unauthorized();

        try
        {
            var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == currentUserId, ct);
            if (currentUser == null)
                return Unauthorized();

            if (currentUser.Role == AppRole.Admin)
            {
                if (!currentUser.DepartmentId.HasValue)
                    return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Admin does not belong to any department.");

                var targetUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
                if (targetUser == null)
                    return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Employee not found.");

                if (targetUser.DepartmentId != currentUser.DepartmentId)
                    return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Admin can update attendance only within their department.");
            }

            var attendance = await _attendanceService.RecordManualAttendanceAsync(request, ct);
            var response = new AttendanceResponse
            {
                Id = attendance.Id,
                UserId = attendance.UserId,
                Date = attendance.Date,
                AttendanceTime = attendance.AttendanceTime,
                DepartureTime = attendance.DepartureTime,
                DeviceType = attendance.DeviceType,
                Location = attendance.Location,
                LocationId = attendance.LocationId,
                CreatedAt = attendance.CreatedAt,
                UpdatedAt = attendance.UpdatedAt
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

    [HttpGet("All")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(AllAttendancesWithSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AllAttendancesWithSummaryResponse>> GetAllAttendances(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] string? machineCode,
        [FromQuery] Guid? employeeId,
        [FromQuery] bool? isCheckIn,
        CancellationToken ct,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            // Get all attendances from database only (no device connection)
            var result = await _attendanceService.GetAllAttendancesWithSummaryAsync(
                startDate,
                endDate,
                machineCode,
                employeeId,
                isCheckIn,
                pageNumber,
                pageSize,
                ct);
            return Ok(result);
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




    [HttpGet("calculate-salary/{employeeId}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(typeof(CalculateSalaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CalculateSalaryResponse>> CalculateSalaryByEmployee(
        Guid employeeId,
        [FromQuery] int month,
        [FromQuery] int year,
        CancellationToken ct)
    {
        try
        {
            var request = new CalculateSalaryRequest
            {
                EmployeeId = employeeId,
                Month = month,
                Year = year
            };

            var result = await _attendanceService.CalculateSalaryAsync(request, ct);
            return Ok(result);
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

    [HttpGet("monthly-report")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMonthlyReport(
        [FromQuery] int month,
        [FromQuery] int year,
        CancellationToken ct)
    {
        try
        {
            var data = await _attendanceService.GetMonthlyReportForAllEmployeesAsync(month, year, ct);

            // Set EPPlus license context (required for non-commercial use)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Create Excel file using EPPlus
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Monthly Report");

            // Set headers
            worksheet.Cells[1, 1].Value = "اسم الموظف";
            worksheet.Cells[1, 2].Value = "القسم";
            worksheet.Cells[1, 3].Value = "المسمى الوظيفي";
            worksheet.Cells[1, 4].Value = "عدد أيام الحضور";
            worksheet.Cells[1, 5].Value = "عدد أيام الغياب";
            worksheet.Cells[1, 6].Value = "إجازة سنوية";
            worksheet.Cells[1, 7].Value = "إجازة عارضة";
            worksheet.Cells[1, 8].Value = "إجازة مرضية";
            worksheet.Cells[1, 9].Value = "إجمالي ساعات العمل";
            worksheet.Cells[1, 10].Value = "إجمالي الساعات المطلوبة";
            worksheet.Cells[1, 11].Value = "عدد الساعات المخصومة";
            worksheet.Cells[1, 12].Value = "الراتب الإجمالي";
            worksheet.Cells[1, 13].Value = "الراتب الصافي";
            worksheet.Cells[1, 14].Value = "الراتب بعد الخصومات";
            worksheet.Cells[1, 15].Value = "إجمالي ساعات الإضافي";
            worksheet.Cells[1, 16].Value = "راتب الإضافي";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 16])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // Add data
            int row = 2;
            foreach (var item in data)
            {
                worksheet.Cells[row, 1].Value = item.Name;
                worksheet.Cells[row, 2].Value = item.DepartmentName ?? "";
                worksheet.Cells[row, 3].Value = item.JobTitle ?? "";
                worksheet.Cells[row, 4].Value = item.TotalAttendance;
                worksheet.Cells[row, 5].Value = item.TotalAbsence;
                worksheet.Cells[row, 6].Value = item.AnnualLeave;
                worksheet.Cells[row, 7].Value = item.CasualLeave;
                worksheet.Cells[row, 8].Value = item.SickLeave;
                worksheet.Cells[row, 9].Value = item.TotalWorkedHoursInMonth;
                worksheet.Cells[row, 10].Value = item.TotalHours;
                worksheet.Cells[row, 11].Value = item.HoursDeducted;
                worksheet.Cells[row, 12].Value = item.GrossSalary;
                worksheet.Cells[row, 13].Value = item.NetSalary;
                worksheet.Cells[row, 14].Value = item.SalaryAfterDeduction;
                worksheet.Cells[row, 15].Value = item.TotalOvertime;
                worksheet.Cells[row, 16].Value = item.OvertimeSalary;
                row++;
            }

            // Totals row
            var totalRow = row;
            worksheet.Cells[totalRow, 1].Value = "الإجمالي";
            worksheet.Cells[totalRow, 13].Value = data.Sum(x => x.NetSalary);

            using (var range = worksheet.Cells[totalRow, 1, totalRow, 16])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
            }

            // Auto-fit columns
            if (worksheet.Dimension != null)
            {
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            }

            // Convert to byte array
            var excelBytes = package.GetAsByteArray();

            // Return file
            var fileName = $"MonthlyReport_{year}_{month:D2}.xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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

    [HttpGet("monthly-report-details")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMonthlyReportDetails(
        [FromQuery] int month,
        [FromQuery] int year,
        CancellationToken ct)
    {
        try
        {
            var data = await _attendanceService.GetMonthlyReportDetailedForAllEmployeesAsync(month, year, ct);
            return Ok(data);
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

    [HttpGet("monthly-report-details-excel")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportMonthlyReportDetailsToExcel(
        [FromQuery] int month,
        [FromQuery] int year,
        CancellationToken ct)
    {
        try
        {
            var data = await _attendanceService.GetMonthlyReportDetailedForAllEmployeesAsync(month, year, ct);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Monthly Report Details");

            var headers = new[]
            {
                "اسم الموظف",
                "القسم",
                "المسمى الوظيفي",
                "الراتب الإجمالي (Gross)",
                "الراتب بعد الخصم (Net)",
                "نظام العمل",
                "سعر الشيفت",
                "بدل السكن",
                "بدل الوجبة",
                "بدل المواصلات",
                "بدل تأميني",
                "البونص",
                "الراتب التأميني",
                "عدد أيام الشهر (الشغل)",
                "عدد أيام الغياب",
                "تواريخ أيام الغياب",
                "إجمالي ساعات العمل",
                "عدد ساعات الخصم",
                "قيمة خصم التأخير",
                "قيمة خصم الغياب",
                "قيمة خصم السلفة",
                "قيمة خصم التأمين الصحي",
                "تواريخ الخصم (التأخير)"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }

            using (var range = worksheet.Cells[1, 1, 1, headers.Length])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            int row = 2;
            foreach (var item in data)
            {
                worksheet.Cells[row, 1].Value = item.Name;
                worksheet.Cells[row, 2].Value = item.DepartmentName ?? "";
                worksheet.Cells[row, 3].Value = item.JobTitle ?? "";
                worksheet.Cells[row, 4].Value = item.GrossSalary;
                worksheet.Cells[row, 5].Value = item.NetSalary;
                worksheet.Cells[row, 6].Value = item.EmploymentMode ?? "";
                worksheet.Cells[row, 7].Value = item.ShiftRate;
                worksheet.Cells[row, 8].Value = item.HousingAllowance;
                worksheet.Cells[row, 9].Value = item.MealAllowance;
                worksheet.Cells[row, 10].Value = item.TransportationAllowance;
                worksheet.Cells[row, 11].Value = item.InsuranceAllowance;
                worksheet.Cells[row, 12].Value = item.BonusAmount;
                worksheet.Cells[row, 13].Value = item.InsuranceSalary;
                worksheet.Cells[row, 14].Value = item.TotalWorkingDays;
                worksheet.Cells[row, 15].Value = item.TotalAbsence;
                worksheet.Cells[row, 16].Value = string.Join(", ", item.AbsenceDates);
                worksheet.Cells[row, 17].Value = item.TotalWorkedHoursInMonth;
                worksheet.Cells[row, 18].Value = item.HoursDeducted;
                worksheet.Cells[row, 19].Value = item.LateDeductionAmount;
                worksheet.Cells[row, 20].Value = item.AbsenceDeductionAmount;
                worksheet.Cells[row, 21].Value = item.AdvanceDeductionAmount;
                worksheet.Cells[row, 22].Value = item.HealthInsuranceDeductionAmount;
                worksheet.Cells[row, 23].Value = string.Join(", ", item.LateDates);
                row++;
            }

            // Totals row
            var totalRow = row;
            worksheet.Cells[totalRow, 1].Value = "الإجمالي";
            worksheet.Cells[totalRow, 5].Value = data.Sum(x => x.NetSalary);

            using (var range = worksheet.Cells[totalRow, 1, totalRow, headers.Length])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
            }

            if (worksheet.Dimension != null)
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var excelBytes = package.GetAsByteArray();
            var fileName = $"MonthlyReport_Details_{year}_{month:D2}.xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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

    [HttpGet("shift-report-excel")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportShiftReportToExcel(
        [FromQuery] int month,
        [FromQuery] int year,
        CancellationToken ct)
    {
        try
        {
            var data = await _attendanceService.GetShiftMonthlyReportItemsAsync(month, year, ct);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Shift Report");

            var headers = new[]
            {
                "اسم الموظف",
                "القسم",
                "المسمى الوظيفي",
                "سعر الشيفت",
                "عدد أيام الشيفت",
                "إجمالي ساعات العمل",
                "الراتب الشهري",
                "التاريخ",
                "وقت الحضور",
                "وقت الانصراف",
                "ساعات العمل لليوم"
            };

            for (int i = 0; i < headers.Length; i++)
                worksheet.Cells[1, i + 1].Value = headers[i];

            using (var range = worksheet.Cells[1, 1, 1, headers.Length])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            int row = 2;
            foreach (var item in data)
            {
                worksheet.Cells[row, 1].Value = item.Name;
                worksheet.Cells[row, 2].Value = item.DepartmentName ?? "";
                worksheet.Cells[row, 3].Value = item.JobTitle ?? "";
                worksheet.Cells[row, 4].Value = item.ShiftRate;
                worksheet.Cells[row, 5].Value = item.TotalShiftDays;
                worksheet.Cells[row, 6].Value = item.TotalWorkedHours;
                worksheet.Cells[row, 7].Value = item.MonthlySalary;
                worksheet.Cells[row, 8].Value = item.Date.ToString("dd/MM/yyyy");
                worksheet.Cells[row, 9].Value = item.AttendanceTime?.ToString("HH:mm") ?? "";
                worksheet.Cells[row, 10].Value = item.DepartureTime?.ToString("HH:mm") ?? "";
                worksheet.Cells[row, 11].Value = item.DayWorkedHours;
                row++;
            }

            if (worksheet.Dimension != null)
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var excelBytes = package.GetAsByteArray();
            var fileName = $"ShiftReport_{year}_{month:D2}.xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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

    [HttpGet("shift-report")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetShiftReport(
        [FromQuery] int month,
        [FromQuery] int year,
        CancellationToken ct)
    {
        try
        {
            var data = await _attendanceService.GetShiftMonthlyReportItemsAsync(month, year, ct);
            return Ok(data);
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
}
