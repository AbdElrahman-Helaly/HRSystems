using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class RegisterFcmTokenRequest
{
    [Required]
    public required string Token { get; init; }
    
    public string? DeviceInfo { get; init; }
}

