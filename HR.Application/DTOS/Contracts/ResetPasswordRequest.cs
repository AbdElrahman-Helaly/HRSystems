namespace internalEmployee.Auth.Contracts;

public sealed class ResetPasswordRequest
{
    public required string PhoneNumber { get; init; }
    public required string Otp { get; init; }
    public required string NewPassword { get; init; }
    public required string ConfirmNewPassword { get; init; }
}

