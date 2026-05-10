using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class OvertimeRequest
{
    [Required]
    public DateOnly Date { get; init; }

    [Required]
    public TimeOnly StartTime { get; init; }

    [Required]
    public TimeOnly EndTime { get; init; }

    [Required, MinLength(5)]
    public string Reason { get; init; } = string.Empty;
}
