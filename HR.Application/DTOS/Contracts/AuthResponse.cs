namespace internalEmployee.Auth.Contracts;

public sealed class AuthResponse
{
    public required string AccessToken { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required string Role { get; init; }
}


