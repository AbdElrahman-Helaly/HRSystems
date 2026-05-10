using System;

namespace internalEmployee.Services.Meeting;

public sealed class MeetingResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateOnly MeetingDate { get; set; }
    public TimeOnly MeetingTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string TargetDepartment { get; set; } = "All";
}
