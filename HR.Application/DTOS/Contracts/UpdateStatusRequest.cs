using System.ComponentModel.DataAnnotations;
using internalEmployee.Data.Entities;

namespace internalEmployee.Auth.Contracts;

public sealed class UpdateStatusRequest
{
    [Required]
    public RequestStatus Status { get; init; }

    public string? RejectionReason { get; init; }
}

