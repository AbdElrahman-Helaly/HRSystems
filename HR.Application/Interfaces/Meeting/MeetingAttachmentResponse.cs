using System;

namespace internalEmployee.Services.Meeting;

public sealed class MeetingAttachmentResponse
{
    public Guid Id { get; set; }
    public int MeetingId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public Guid UploadedByUserId { get; set; }
}
