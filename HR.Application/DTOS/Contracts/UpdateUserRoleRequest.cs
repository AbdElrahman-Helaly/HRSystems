using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class UpdateUserRoleRequest
{
    [Required]
    public string Role { get; init; } = string.Empty;
}

