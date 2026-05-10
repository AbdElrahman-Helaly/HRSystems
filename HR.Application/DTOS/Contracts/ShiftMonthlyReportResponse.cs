namespace internalEmployee.Auth.Contracts;

public sealed class ShiftMonthlyReportResponse
{
    public Guid UserId { get; init; }
    public required string Name { get; init; }
    public string? DepartmentName { get; init; }
    public string? JobTitle { get; init; }
    public decimal ShiftRate { get; init; }
    public int PaidShiftDays { get; init; }
    public decimal TotalWorkedHours { get; init; }
    public decimal Salary { get; init; }
    public List<string> ShiftDates { get; init; } = new();
}
