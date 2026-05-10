using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class AssignmentRequest
{
    [Required, MinLength(3)]
    public string Where { get; init; } = string.Empty;

    [Required]
    public DateOnly StartDate { get; init; }

    [Required]
    public DateOnly EndDate { get; init; }

    [Required]
    public TimeOnly StartTime { get; init; }

    [Required]
    public TimeOnly EndTime { get; init; }

    public string? Reason { get; init; }
}

