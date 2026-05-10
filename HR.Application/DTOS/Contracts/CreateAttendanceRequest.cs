using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class CreateAttendanceRequest
{
    [Required]
    public DateOnly Date { get; init; }

    public TimeOnly? AttendanceTime { get; init; } // Optional, defaults to current time if not provided
}

