using internalEmployee.Auth.Models;

namespace internalEmployee.Data.Entities;

public sealed class PublicHolidayException
{
    public int Id { get; set; }
    public int PublicHolidayId { get; set; }
    public Guid? EmployeeId { get; set; }
    public int? DepartmentId { get; set; }
    public int? EmploymentModeId { get; set; }
    public Religion? Religion { get; set; }

    public PublicHoliday PublicHoliday { get; set; } = null!;
    public AppUser? Employee { get; set; }
    public Department? Department { get; set; }
    public EmploymentMode? EmploymentMode { get; set; }
}
