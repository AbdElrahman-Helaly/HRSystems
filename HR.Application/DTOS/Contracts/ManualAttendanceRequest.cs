using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class ManualAttendanceRequest
{
    [Required]
    public Guid UserId { get; init; }

    [Required]
    public DateOnly Date { get; init; }

    public TimeOnly? AttendanceTime { get; init; } // Check-in
    public TimeOnly? DepartureTime { get; init; } // Check-out
}
