using internalEmployee.Auth.Contracts;
using internalEmployee.Services.Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Security.Claims;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PayslipController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public PayslipController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
        QuestPDF.Settings.License = LicenseType.Community;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(PayslipResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PayslipResponse>> GetMyPayslip([FromQuery] int month, [FromQuery] int year, CancellationToken ct)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var result = await _attendanceService.GetPayslipAsync(userId, month, year, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpGet("my/excel")]
    public async Task<IActionResult> ExportMyPayslipToExcel([FromQuery] int month, [FromQuery] int year, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        return await ExportEmployeePayslipToExcel(userId, month, year, ct);
    }

    [HttpGet("my/pdf")]
    public async Task<IActionResult> ExportMyPayslipToPdf([FromQuery] int month, [FromQuery] int year, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        return await ExportEmployeePayslipToPdf(userId, month, year, ct);
    }

    [HttpGet("employee/{id}")]
    [Authorize(Roles = "HR,Admin,SuperAdmin")]
    public async Task<ActionResult<PayslipResponse>> GetEmployeePayslip(Guid id, [FromQuery] int month, [FromQuery] int year, CancellationToken ct)
    {
        try
        {
            var result = await _attendanceService.GetPayslipAsync(id, month, year, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
                return NotFound();
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpGet("employee/{id}/excel")]
    [Authorize(Roles = "HR,Admin,SuperAdmin")]
    public async Task<IActionResult> ExportEmployeePayslipToExcel(Guid id, [FromQuery] int month, [FromQuery] int year, CancellationToken ct)
    {
        try
        {
            var payslip = await _attendanceService.GetPayslipAsync(id, month, year, ct);

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Payslip");

            // Header - Styles
            worksheet.Cells["A1:B1"].Merge = true;
            worksheet.Cells["A1"].Value = "بيان مفردات الراتب (Payslip)";
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells["A1"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

            // Employee Information
            worksheet.Cells["A3"].Value = "الاسم (عربي):";
            worksheet.Cells["B3"].Value = payslip.FullNameAr;
            worksheet.Cells["A4"].Value = "Name (En):";
            worksheet.Cells["B4"].Value = payslip.FullNameEn;
            worksheet.Cells["A5"].Value = "القسم:";
            worksheet.Cells["B5"].Value = payslip.DepartmentName;
            worksheet.Cells["A6"].Value = "الوظيفة:";
            worksheet.Cells["B6"].Value = payslip.JobTitle;
            worksheet.Cells["A7"].Value = "نظام العمل:";
            worksheet.Cells["B7"].Value = payslip.EmploymentMode ?? "";
            worksheet.Cells["A8"].Value = "الفترة:";
            worksheet.Cells["B8"].Value = $"{payslip.Month:D2} / {payslip.Year}";
            worksheet.Cells["A9"].Value = "أيام العمل الفعلية:";
            worksheet.Cells["B9"].Value = payslip.ActualWorkingDays;
            worksheet.Cells["A10"].Value = "تاريخ الإصدار:";
            worksheet.Cells["B10"].Value = payslip.IssuedAt.ToString("dd/MM/yyyy");

            worksheet.Cells["A3:A10"].Style.Font.Bold = true;

            worksheet.Cells["A11"].Value = "عدد أيام الشهر (الشغل):";
            worksheet.Cells["B11"].Value = payslip.SalaryDetails.TotalWorkingDays;
            worksheet.Cells["A12"].Value = "إجمالي ساعات العمل:";
            worksheet.Cells["B12"].Value = payslip.SalaryDetails.HoursWorked;
            worksheet.Cells["A13"].Value = "عدد أيام الغياب:";
            worksheet.Cells["B13"].Value = payslip.SalaryDetails.Deductions.AbsenceDays;
            worksheet.Cells["A14"].Value = "تواريخ الغياب:";
            worksheet.Cells["B14"].Value = string.Join(", ", payslip.SalaryDetails.AbsenceDates);
            worksheet.Cells["A15"].Value = "ساعات التأخير:";
            worksheet.Cells["B15"].Value = payslip.SalaryDetails.Deductions.LateHours;
            worksheet.Cells["A16"].Value = "تواريخ التأخير:";
            worksheet.Cells["B16"].Value = string.Join(", ", payslip.SalaryDetails.LateDates);
            worksheet.Cells["A17"].Value = "سعر الشيفت:";
            worksheet.Cells["B17"].Value = payslip.SalaryDetails.ShiftRate ?? 0m;
            worksheet.Cells["A18"].Value = "أيام الشيفت المدفوعة:";
            worksheet.Cells["B18"].Value = payslip.SalaryDetails.PaidShiftDays;

            // Salary Details Header
            worksheet.Cells["D3"].Value = "التفاصيل المالية (Financial Details)";
            worksheet.Cells["D3:E3"].Merge = true;
            worksheet.Cells["D3"].Style.Font.Bold = true;
            worksheet.Cells["D3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells["D3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells["D3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.AliceBlue);

            // Earnings
            int row = 4;
            worksheet.Cells[row, 4].Value = "الراتب الأساسي (Gross Salary):";
            worksheet.Cells[row++, 5].Value = payslip.SalaryDetails.GrossSalary;

            worksheet.Cells[row, 4].Value = "بدل سكن (Housing):";
            worksheet.Cells[row++, 5].Value = payslip.SalaryDetails.Allowances.Housing;

            worksheet.Cells[row, 4].Value = "بدل وجبة (Meal):";
            worksheet.Cells[row++, 5].Value = payslip.SalaryDetails.Allowances.Meal;

            worksheet.Cells[row, 4].Value = "بدل مواصلات (Transport):";
            worksheet.Cells[row++, 5].Value = payslip.SalaryDetails.Allowances.Transportation;

            worksheet.Cells[row, 4].Value = "بدل تأميني (Insurance):";
            worksheet.Cells[row++, 5].Value = payslip.SalaryDetails.Allowances.Insurance;

            worksheet.Cells[row, 4].Value = "إضافي (Overtime):";
            worksheet.Cells[row++, 5].Value = payslip.SalaryDetails.OvertimePay;

            worksheet.Cells[row, 4].Value = "مكافآت (Bonuses):";
            worksheet.Cells[row++, 5].Value = payslip.SalaryDetails.BonusAmount;

            worksheet.Cells[row, 4].Value = "الراتب التأميني (Insurance Salary):";
            worksheet.Cells[row++, 5].Value = payslip.SalaryDetails.Insurance.InsuranceSalary;

            var totalEarningsRow = row;
            worksheet.Cells[row, 4].Value = "إجمالي الاستحقاق (Total Earnings):";
            worksheet.Cells[row++, 5].Value = payslip.SalaryDetails.TotalEarnings;
            worksheet.Cells[totalEarningsRow, 4, totalEarningsRow, 5].Style.Font.Bold = true;

            // Deductions
            row++;
            worksheet.Cells[row, 4].Value = "الخصومات (Deductions)";
            worksheet.Cells[row, 4, row, 5].Merge = true;
            worksheet.Cells[row, 4, row, 5].Style.Font.Bold = true;
            worksheet.Cells[row, 4, row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[row, 4, row, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[row, 4, row, 5].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.MistyRose);
            row++;

            worksheet.Cells[row, 4].Value = "غياب (Absence):";
            worksheet.Cells[row++, 5].Value = payslip.SalaryDetails.Deductions.AbsenceAmount;

            worksheet.Cells[row, 4].Value = "تأخير (Late):";
            worksheet.Cells[row++, 5].Value = payslip.SalaryDetails.Deductions.LateAmount;

            worksheet.Cells[row, 4].Value = "جزاءات (Penalties):";
            worksheet.Cells[row++, 5].Value = payslip.SalaryDetails.Deductions.PenaltiesAmount;

            worksheet.Cells[row, 4].Value = "سلفة (Advance):";
            worksheet.Cells[row++, 5].Value = payslip.SalaryDetails.Deductions.AdvancesAmount;

            worksheet.Cells[row, 4].Value = "تأمين صحي (Health Insurance):";
            worksheet.Cells[row++, 5].Value = payslip.SalaryDetails.Deductions.HealthInsuranceAmount;

            worksheet.Cells[row, 4].Value = "تأمين اجتماعي (Social Insurance):";
            worksheet.Cells[row++, 5].Value = payslip.SalaryDetails.Insurance.Social;

            worksheet.Cells[row, 4].Value = "ضرائب (Tax):";
            worksheet.Cells[row++, 5].Value = payslip.SalaryDetails.TaxAmount;

            var totalDeductionsRow = row;
            worksheet.Cells[row, 4].Value = "إجمالي الخصومات (Total Deductions):";
            worksheet.Cells[row++, 5].Value = payslip.SalaryDetails.Deductions.Total + payslip.SalaryDetails.Insurance.TotalDeducted + payslip.SalaryDetails.TaxAmount;
            worksheet.Cells[totalDeductionsRow, 4, totalDeductionsRow, 5].Style.Font.Bold = true;

            // Net Salary
            row++;
            worksheet.Cells[row, 4].Value = "صافي الراتب (Net Salary):";
            worksheet.Cells[row, 5].Value = payslip.SalaryDetails.NetSalary;
            worksheet.Cells[row, 4, row, 5].Style.Font.Bold = true;
            worksheet.Cells[row, 4, row, 5].Style.Font.Size = 14;
            worksheet.Cells[row, 4, row, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[row, 4, row, 5].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);

            worksheet.Cells.AutoFitColumns();

            var excelBytes = package.GetAsByteArray();
            var fileName = $"Payslip_{id}_{year}_{month:D2}.xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: ex.Message);
        }
    }

    [HttpGet("employee/{id}/pdf")]
    [Authorize(Roles = "HR,Admin,SuperAdmin")]
    public async Task<IActionResult> ExportEmployeePayslipToPdf(Guid id, [FromQuery] int month, [FromQuery] int year, CancellationToken ct)
    {
        try
        {
            var payslip = await _attendanceService.GetPayslipAsync(id, month, year, ct);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20, Unit.Millimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Black));

                    // Professional Branded Header (Mediconsult Style) - Single Definition
                    page.Header().Column(header =>
                    {
                        // ===== HERO HEADER =====
                        header.Item().Height(120).Layers(layers =>
                        {
                            // ===== Background Blue Shape =====
                            layers.Layer().Element(container =>
                            {
                                container.Row(row =>
                                {
                                    row.ConstantItem(320).Element(box =>
                                    {
                                        box.Background("#1E2A78")
                                           .CornerRadius(40)
                                           .Padding(20)
                                           .Column(col =>
                                           {
                                               var logoPath = Path.Combine(
                                                   Directory.GetCurrentDirectory(),
                                                   "wwwroot",
                                                   "images",
                                                   "logo.png");

                                               if (System.IO.File.Exists(logoPath))
                                               {
                                                   col.Item().Height(50).Image(logoPath);
                                               }
                                               else
                                               {
                                                   col.Item().Text("Mediconsult")
                                                       .FontSize(20)
                                                       .Bold()
                                                       .FontColor(Colors.White);
                                               }
                                           });
                                    });

                                    row.RelativeItem();
                                });
                            });

                            // ===== Title Layer =====
                            layers.PrimaryLayer().Element(container =>
                            {
                                container.Row(row =>
                                {
                                    row.RelativeItem();

                                    row.ConstantItem(260).AlignMiddle().Column(col =>
                                    {
                                        col.Item().AlignRight().Text("PAYSLIP")
                                            .FontSize(28)
                                            .Bold()
                                            .FontColor("#1E2A78");

                                        col.Item().AlignRight().Text("بيان مفردات الراتب")
                                            .FontSize(12)
                                            .FontColor("#6B7280");

                                        col.Item().AlignRight().Text($"{payslip.Month:D2} / {payslip.Year}")
                                            .FontSize(10)
                                            .FontColor("#9CA3AF");
                                    });
                                });
                            });
                        });

                        // ===== Divider Line =====
                        header.Item().PaddingTop(5)
                            .LineHorizontal(1)
                            .LineColor("#E5E7EB");
                    });

                    page.Content().PaddingTop(20).Column(col =>
                    {
                        col.Spacing(15);

                        // Employee Information Section
                        col.Item().Background(Colors.Grey.Lighten4).Padding(12).Column(section =>
                        {
                            section.Item().BorderBottom(1).BorderColor(Colors.Blue.Darken2).PaddingBottom(5)
                                .Text("معلومات الموظف | Employee Information").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
                            
                            section.Item().PaddingTop(10).Row(row =>
                            {
                                // Right column (Arabic)
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Row(r =>
                                    {
                                        r.ConstantItem(100).Text("الاسم (عربي):").SemiBold().FontSize(9);
                                        r.RelativeItem().Text(payslip.FullNameAr ?? "-").FontSize(9);
                                    });
                                    c.Item().PaddingTop(5).Row(r =>
                                    {
                                        r.ConstantItem(100).Text("Name (English):").SemiBold().FontSize(9);
                                        r.RelativeItem().Text(payslip.FullNameEn ?? "-").FontSize(9);
                                    });
                                    c.Item().PaddingTop(5).Row(r =>
                                    {
                                        r.ConstantItem(100).Text("القسم | Department:").SemiBold().FontSize(9);
                                        r.RelativeItem().Text(payslip.DepartmentName ?? "-").FontSize(9);
                                    });
                                    c.Item().PaddingTop(5).Row(r =>
                                    {
                                        r.ConstantItem(100).Text("نظام العمل | Mode:").SemiBold().FontSize(9);
                                        r.RelativeItem().Text(payslip.EmploymentMode ?? "-").FontSize(9);
                                    });
                                });

                                row.ConstantItem(30);

                                // Left column
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Row(r =>
                                    {
                                        r.ConstantItem(120).Text("الوظيفة | Job Title:").SemiBold().FontSize(9);
                                        r.RelativeItem().Text(payslip.JobTitle ?? "-").FontSize(9);
                                    });
                                    c.Item().PaddingTop(5).Row(r =>
                                    {
                                        r.ConstantItem(120).Text("أيام العمل | Working Days:").SemiBold().FontSize(9);
                                        r.RelativeItem().Text($"{payslip.ActualWorkingDays} days").FontSize(9);
                                    });
                                    c.Item().PaddingTop(5).Row(r =>
                                    {
                                        r.ConstantItem(120).Text("أيام الشهر (الشغل):").SemiBold().FontSize(9);
                                        r.RelativeItem().Text($"{payslip.SalaryDetails.TotalWorkingDays}").FontSize(9);
                                    });
                                    c.Item().PaddingTop(5).Row(r =>
                                    {
                                        r.ConstantItem(120).Text("ساعات العمل:").SemiBold().FontSize(9);
                                        r.RelativeItem().Text($"{payslip.SalaryDetails.HoursWorked:N2}").FontSize(9);
                                    });
                                    c.Item().PaddingTop(5).Row(r =>
                                    {
                                        r.ConstantItem(120).Text("سعر الشيفت:").SemiBold().FontSize(9);
                                        r.RelativeItem().Text($"{(payslip.SalaryDetails.ShiftRate ?? 0m):N2}").FontSize(9);
                                    });
                                    c.Item().PaddingTop(5).Row(r =>
                                    {
                                        r.ConstantItem(120).Text("أيام الشيفت المدفوعة:").SemiBold().FontSize(9);
                                        r.RelativeItem().Text($"{payslip.SalaryDetails.PaidShiftDays}").FontSize(9);
                                    });
                                });
                            });
                        });

                        // Financial Details - Two Column Layout
                        col.Item().Row(row =>
                        {
                            // Earnings Column (Green)
                            row.RelativeItem().Column(earnings =>
                            {
                                earnings.Item().Background(Colors.Green.Lighten4).Padding(8).Text("الاستحقاقات | Earnings")
                                    .FontSize(11).Bold().FontColor(Colors.Green.Darken3);
                                
                                earnings.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Table(table =>
                                {
                                    table.ColumnsDefinition(c =>
                                    {
                                        c.RelativeColumn(2);
                                        c.RelativeColumn(1);
                                    });

                                    // Header
                                    table.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("البند | Item").FontSize(9).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text("المبلغ | Amount").FontSize(9).SemiBold();

                                    // Rows with alternating colors
                                    AddStyledTableRow(table, "الراتب الأساسي | Base Salary", payslip.SalaryDetails.GrossSalary, true);
                                    AddStyledTableRow(table, "بدل سكن | Housing", payslip.SalaryDetails.Allowances.Housing, false);
                                    AddStyledTableRow(table, "بدل وجبة | Meal", payslip.SalaryDetails.Allowances.Meal, true);
                                    AddStyledTableRow(table, "بدل مواصلات | Transport", payslip.SalaryDetails.Allowances.Transportation, false);
                                    AddStyledTableRow(table, "بدل تأميني | Insurance", payslip.SalaryDetails.Allowances.Insurance, true);
                                    AddStyledTableRow(table, "إضافي | Overtime", payslip.SalaryDetails.OvertimePay, true);
                                    AddStyledTableRow(table, "مكافآت | Bonuses", payslip.SalaryDetails.BonusAmount, false);

                                    // Total
                                    table.Cell().Background(Colors.Green.Medium).Padding(6).Text("الإجمالي | Total").Bold().FontColor(Colors.White).FontSize(10);
                                    table.Cell().Background(Colors.Green.Medium).Padding(6).AlignRight().Text($"{payslip.SalaryDetails.TotalEarnings:N2} EGP").Bold().FontColor(Colors.White).FontSize(10);
                                });
                            });

                            row.ConstantItem(20);

                            // Deductions Column (Red)
                            row.RelativeItem().Column(deductions =>
                            {
                                deductions.Item().Background(Colors.Red.Lighten4).Padding(8).Text("الاستقطاعات | Deductions")
                                    .FontSize(11).Bold().FontColor(Colors.Red.Darken3);
                                
                                deductions.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Table(table =>
                                {
                                    table.ColumnsDefinition(c =>
                                    {
                                        c.RelativeColumn(2);
                                        c.RelativeColumn(1);
                                    });

                                    // Header
                                    table.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("البند | Item").FontSize(9).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text("المبلغ | Amount").FontSize(9).SemiBold();

                                    // Rows with alternating colors
                                    AddStyledTableRow(table, "غياب | Absence", payslip.SalaryDetails.Deductions.AbsenceAmount, true);
                                    AddStyledTableRow(table, "تأخير | Late", payslip.SalaryDetails.Deductions.LateAmount, false);
                                    AddStyledTableRow(table, "جزاءات | Penalties", payslip.SalaryDetails.Deductions.PenaltiesAmount, true);
                                    AddStyledTableRow(table, "سلفة | Advance", payslip.SalaryDetails.Deductions.AdvancesAmount, false);
                                    AddStyledTableRow(table, "تأمين صحي | Health Insurance", payslip.SalaryDetails.Deductions.HealthInsuranceAmount, true);
                                    if (payslip.SalaryDetails.Insurance.Social > 0)
                                    {
                                        AddStyledTableRow(table, "أساس التأمين | Insurance Base", payslip.SalaryDetails.Insurance.InsuranceSalary, false);
                                    }
                                    AddStyledTableRow(table, "تأمين اجتماعي | Insurance", payslip.SalaryDetails.Insurance.Social, true);
                                    AddStyledTableRow(table, "ضرائب | Tax", payslip.SalaryDetails.TaxAmount, false);

                                    decimal totalDeds = payslip.SalaryDetails.Deductions.Total + 
                                                       payslip.SalaryDetails.Insurance.TotalDeducted + 
                                                       payslip.SalaryDetails.TaxAmount;

                                    // Total
                                    table.Cell().Background(Colors.Red.Medium).Padding(6).Text("الإجمالي | Total").Bold().FontColor(Colors.White).FontSize(10);
                                    table.Cell().Background(Colors.Red.Medium).Padding(6).AlignRight().Text($"{totalDeds:N2} EGP").Bold().FontColor(Colors.White).FontSize(10);
                                });
                            });
                        });

                        col.Item().PaddingTop(20);

                        // Attendance Summary
                        col.Item().Background(Colors.Grey.Lighten4).Padding(8).Column(summary =>
                        {
                            summary.Item().Text("ملخص الحضور | Attendance Summary").FontSize(11).Bold();
                            summary.Item().PaddingTop(4).Text($"أيام الغياب: {payslip.SalaryDetails.Deductions.AbsenceDays}").FontSize(9);
                            summary.Item().PaddingTop(2).Text($"تواريخ الغياب: {string.Join(", ", payslip.SalaryDetails.AbsenceDates)}").FontSize(9);
                            summary.Item().PaddingTop(4).Text($"ساعات التأخير: {payslip.SalaryDetails.Deductions.LateHours:N2}").FontSize(9);
                            summary.Item().PaddingTop(2).Text($"تواريخ التأخير: {string.Join(", ", payslip.SalaryDetails.LateDates)}").FontSize(9);
                        });

                        col.Item().PaddingTop(10);

                        // Net Salary - Bottom Bar
                        col.Item().Height(50).Background(Colors.Blue.Darken2).Padding(12).Row(row =>
                        {
                            row.RelativeItem().AlignMiddle().Text("صافي الراتب | Net Salary").FontSize(18).Bold().FontColor(Colors.White);
                            row.ConstantItem(180).AlignRight().AlignMiddle().Text($"{payslip.SalaryDetails.NetSalary:N2} EGP").FontSize(20).Bold().FontColor(Colors.White);
                        });
                    });

                    // Footer
                    page.Footer().AlignCenter().Column(col =>
                    {
                        col.Item().PaddingBottom(5).LineHorizontal(0.5f).LineColor(Colors.Blue.Darken2);
                        col.Item().PaddingTop(8).Text(text =>
                        {
                            text.Span("This is a system-generated document. ").FontSize(8).FontColor(Colors.Grey.Medium);
                            text.Span($"Generated on: {DateTime.Now:dd/MM/yyyy HH:mm} UTC").FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();
            var fileName = $"Payslip_{payslip.FullNameEn?.Replace(" ", "_")}_{year}_{month:D2}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: ex.Message);
        }
    }

    private static void AddStyledTableRow(TableDescriptor table, string label, decimal amount, bool isAlternate)
    {
        var bgColor = isAlternate ? Colors.White : Colors.Grey.Lighten5;
        table.Cell().Background(bgColor).Padding(6).Text(label).FontSize(9);
        table.Cell().Background(bgColor).Padding(6).AlignRight().Text($"{amount:N2}").FontSize(9);
    }
}
