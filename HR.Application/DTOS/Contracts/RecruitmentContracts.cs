using System.ComponentModel.DataAnnotations;
using internalEmployee.Data.Entities;
using Microsoft.AspNetCore.Http;

namespace internalEmployee.Auth.Contracts;

public sealed class CreateRecruitmentRequest
{
    [Required]
    [MaxLength(200)]
    public string RequestedJobTitle { get; init; } = string.Empty;

    [Range(1, 100)]
    public int RequiredCount { get; init; }

    [Range(0, 50)]
    public int? RequiredExperienceYears { get; init; }

    [MaxLength(1000)]
    public string? Skills { get; init; }

    [MaxLength(2000)]
    public string? Description { get; init; }
}

public sealed class UpdateRecruitmentRequestStatusRequest
{
    [Required]
    public RequestStatus Status { get; init; }

    [MaxLength(1000)]
    public string? Note { get; init; }
}

public sealed class RecruitmentRequestResponse
{
    public int Id { get; init; }
    public Guid RequestedByUserId { get; init; }
    public string? RequesterFullNameAr { get; init; }
    public string? RequesterFullNameEn { get; init; }
    public int? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public string RequestedJobTitle { get; init; } = string.Empty;
    public int RequiredCount { get; init; }
    public int? RequiredExperienceYears { get; init; }
    public string? Skills { get; init; }
    public string? Description { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? HrResponseNote { get; init; }
    public int CandidatesCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class CreateRecruitmentCandidateRequest
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; init; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; init; }

    [MaxLength(200)]
    [EmailAddress]
    public string? Email { get; init; }

    [Range(0, 50)]
    public int? ExperienceYears { get; init; }

    [MaxLength(2000)]
    public string? Notes { get; init; }

    [Required]
    public IFormFile CvFile { get; init; } = default!;
}

public sealed class UpdateRecruitmentCandidateStatusRequest
{
    [Required]
    public RequestStatus Status { get; init; }

    [MaxLength(1000)]
    public string? ResponseNote { get; init; }
}

public sealed class RecruitmentCandidateResponse
{
    public int Id { get; init; }
    public int RecruitmentRequestId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? Email { get; init; }
    public int? ExperienceYears { get; init; }
    public string? Notes { get; init; }
    public string CvOriginalFileName { get; init; } = string.Empty;
    public string CvUrl { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? ManagerResponseNote { get; init; }
    public Guid SubmittedByHrUserId { get; init; }
    public string? SubmittedByHrNameAr { get; init; }
    public string? SubmittedByHrNameEn { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class RecruitmentCandidateFileResponse
{
    public string FilePath { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
}
