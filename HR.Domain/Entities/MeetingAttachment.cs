namespace internalEmployee.Data.Entities;

public sealed class MeetingAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int MeetingId { get; set; }
    public Guid UploadedByUserId { get; set; }
    public required string FileName { get; set; }
    public required string OriginalFileName { get; set; }
    public required string FilePath { get; set; }
    public long FileSize { get; set; }
    public required string ContentType { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.Now;

    public Meeting? Meeting { get; set; }
}
