namespace internalEmployee.Auth.Contracts;

public sealed class VerifyResetOtpRequest
{
    public required string PhoneNumber { get; init; }
    public required string Otp { get; init; }
}
