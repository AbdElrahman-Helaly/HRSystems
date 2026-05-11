namespace internalEmployee.Data.Entities;

public sealed class FcmToken
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public required string Token { get; set; }
    public string? DeviceInfo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastUsedAt { get; set; }
}

