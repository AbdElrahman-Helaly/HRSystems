using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Data.Entities;

public sealed class EmployeeEducation
{
    public long Id { get; set; }

    public Guid UserId { get; set; }

    public string? UniversityName { get; set; }
    public DateOnly? GraduationYear { get; set; }
    public string? Degree { get; set; }
    public string? FinalGrade { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public internalEmployee.Auth.Models.AppUser? User { get; set; }
}


