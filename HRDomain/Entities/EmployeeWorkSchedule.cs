namespace internalEmployee.Data.Entities;

public sealed class EmployeeWorkSchedule
{
    // 1:1 with AppUser (use UserId as PK)
    public Guid UserId { get; set; }

    // Used when ScheduleType == PartTime
    public TimeOnly? PartTimeStart { get; set; }
    public TimeOnly? PartTimeEnd { get; set; }
    public bool PartTimeUseDefaultWeek { get; set; } = true;
    public string? PartTimeCustomDaysJson { get; set; }

    // Optional: for future policies (grace, etc.)
    public TimeOnly? FullTimeStartOverride { get; set; }
    public TimeOnly? FullTimeEndOverride { get; set; }
}


