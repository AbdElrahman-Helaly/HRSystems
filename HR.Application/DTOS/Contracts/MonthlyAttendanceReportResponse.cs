namespace internalEmployee.Auth.Contracts;

public sealed class MonthlyAttendanceReportResponse
{
    public Guid UserId { get; init; }
    public int Month { get; init; }
    public int Year { get; init; }

    public decimal TotalLateDeductionHours { get; init; }
    public decimal TotalOvertimeHours { get; init; }
    public decimal? OvertimeRate { get; init; }

    public List<MonthlyAttendanceReportDayItem> Days { get; init; } = new();
}

public sealed class MonthlyAttendanceReportDayItem
{
    public DateOnly Date { get; init; }

    public TimeOnly? ScheduledStartTime { get; init; }
    public TimeOnly? ScheduledEndTime { get; init; }

    public TimeOnly? AttendanceTime { get; init; }
    public TimeOnly? DepartureTime { get; init; }

    public int? LateMinutes { get; init; }
    public string LateDeductionType { get; init; } = "None";
    public decimal LateDeductionHours { get; init; }

    public decimal OvertimeHours { get; init; }
}


