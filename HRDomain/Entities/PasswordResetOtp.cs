namespace internalEmployee.Data.Entities;

public sealed class PasswordResetOtp
{
    public int Id { get; set; }
    public required string PhoneNumber { get; set; }
    public required string OtpHash { get; set; }
    public required string OtpSalt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UsedAt { get; set; }
    public bool IsUsed { get; set; }
    public int FailedAttempts { get; set; }
}
