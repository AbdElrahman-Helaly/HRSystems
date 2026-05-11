namespace internalEmployee.Data.Entities;

public sealed class UserAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required Guid UserId { get; set; }
    public required string FileName { get; set; }
    public required string OriginalFileName { get; set; }
    public required string FilePath { get; set; }
    public long FileSize { get; set; }
    public required string ContentType { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.Now;
}

