namespace internalEmployee.Data.Entities;

public sealed class RecruitmentRequest
{
    public int Id { get; set; }
    public Guid RequestedByUserId { get; set; }
    public int? DepartmentId { get; set; }
    public string RequestedJobTitle { get; set; } = string.Empty;
    public int RequiredCount { get; set; }
    public int? RequiredExperienceYears { get; set; }
    public string? Skills { get; set; }
    public string? Description { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? HrResponseNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}
