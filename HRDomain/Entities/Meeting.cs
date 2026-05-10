using System;
using System.Collections.Generic;

namespace internalEmployee.Data.Entities;

public sealed class Meeting
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateOnly MeetingDate { get; set; }
    public TimeOnly MeetingTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Guid CreatedByUserId { get; set; }
    // Navigation properties
    public ICollection<MeetingDepartment> MeetingDepartments { get; set; } = new List<MeetingDepartment>();
    public ICollection<MeetingAttachment> Attachments { get; set; } = new List<MeetingAttachment>();
}
