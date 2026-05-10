namespace internalEmployee.Auth.Contracts;

public sealed class PublicHolidayExceptionResponse
{
    public int Id { get; init; }
    public int PublicHolidayId { get; init; }
    public Guid? EmployeeId { get; init; }
    public string? EmployeeName { get; init; }
    public int? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public int? EmploymentModeId { get; init; }
    public string? EmploymentModeName { get; init; }
    public internalEmployee.Auth.Models.Religion? Religion { get; init; }
    public string? ReligionName { get; init; }
}
