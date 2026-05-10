namespace internalEmployee.Auth.Contracts;

public sealed class CreatePublicHolidayExceptionRequest
{
    public Guid? EmployeeId { get; init; }
    public int? DepartmentId { get; init; }
    public int? EmploymentModeId { get; init; }
    public internalEmployee.Auth.Models.Religion? Religion { get; init; }
}
