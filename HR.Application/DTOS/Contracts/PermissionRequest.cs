using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class PermissionRequest
{
    [Required]
    public DateOnly Date { get; init; }

    [Required]
    public TimeOnly StartTime { get; init; }

    [Required]
    public TimeOnly EndTime { get; init; }

    public string? Reason { get; init; }
}

