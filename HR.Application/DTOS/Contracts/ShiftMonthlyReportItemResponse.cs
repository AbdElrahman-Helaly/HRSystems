namespace internalEmployee.Auth.Contracts;

public sealed class ShiftMonthlyReportItemResponse
{
    public Guid UserId { get; init; }
    public required string Name { get; init; }
    public string? DepartmentName { get; init; }
    public string? JobTitle { get; init; }

    public decimal ShiftRate { get; init; }
    public int TotalShiftDays { get; init; }
    public decimal TotalWorkedHours { get; init; }
    public decimal MonthlySalary { get; init; }

    public DateOnly Date { get; init; }
    public TimeOnly? AttendanceTime { get; init; }
    public TimeOnly? DepartureTime { get; init; }
    public decimal DayWorkedHours { get; init; }
}
