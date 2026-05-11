namespace internalEmployee.Data.Entities;

public sealed class EmployeeWeeklyShift
{
    public long Id { get; set; }

    public Guid UserId { get; set; }

    // 0..6 (Sunday..Saturday) same as System.DayOfWeek
    public int DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}


