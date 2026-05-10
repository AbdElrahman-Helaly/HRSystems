using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class LoginRequest
{
    [Required]
    public string PhoneNumber { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}


