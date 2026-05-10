using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class CreateDepartureRequest
{
    [Required]
    public DateOnly Date { get; init; }

    public TimeOnly? DepartureTime { get; init; } // Optional, defaults to current time if not provided
}

