using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class AttendanceRequest
{
    [Required]
    public DateOnly Date { get; init; }

    public TimeOnly? AttendanceTime { get; init; }

    public TimeOnly? DepartureTime { get; init; }
}

