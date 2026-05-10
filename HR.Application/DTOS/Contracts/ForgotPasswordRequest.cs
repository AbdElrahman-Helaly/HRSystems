namespace internalEmployee.Auth.Contracts;

public sealed class ForgotPasswordRequest
{
    public required string PhoneNumber { get; init; }
}

