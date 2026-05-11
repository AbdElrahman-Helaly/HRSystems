namespace internalEmployee.Data.Entities;

public sealed class Notification
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public int RequestId { get; set; }
    public required string Message { get; set; }
    public bool IsRead { get; set; } = false;
    public bool IsConfirmed { get; set; } = false;
    public DateTime? ConfirmedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public enum NotificationType
{
    Permission = 1,
    Leave = 2,
    Assignment = 3,
    Meeting = 4,
    Direct = 5,
    Overtime = 6,
    SalaryAdvance = 7,
    RecruitmentRequest = 8,
    RecruitmentCandidate = 9,
    Broadcast = 10,
    WorkFromHome = 11
}

