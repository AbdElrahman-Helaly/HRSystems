using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class UpdateUserPendingStatusRequest
{
    [Required]
    public bool IsPending { get; init; }
}

