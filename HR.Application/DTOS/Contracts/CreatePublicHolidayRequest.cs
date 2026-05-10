using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class CreatePublicHolidayRequest
{
    [Required]
    public DateOnly Date { get; init; }

    [Required, MinLength(1)]
    public string Name { get; init; } = string.Empty;

    public string? NameAr { get; init; }

    [Required]
    [Range(2000, 2100)]
    public int Year { get; init; }

    public bool IsActive { get; init; } = true;
}
