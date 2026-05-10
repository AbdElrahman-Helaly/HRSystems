using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class UpdateAdminPasswordRequest
{
    [Required]
    public required string NationalId { get; init; }

    [Required, MinLength(8)]
    public required string NewPassword { get; init; }
}

