namespace internalEmployee.Data.Entities;

public sealed class RecruitmentCandidate
{
    public int Id { get; set; }
    public int RecruitmentRequestId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public int? ExperienceYears { get; set; }
    public string? Notes { get; set; }
    public string CvFileName { get; set; } = string.Empty;
    public string CvOriginalFileName { get; set; } = string.Empty;
    public string CvFilePath { get; set; } = string.Empty;
    public string CvContentType { get; set; } = string.Empty;
    public long CvFileSize { get; set; }
    public Guid SubmittedByHrUserId { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? ManagerResponseNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}
