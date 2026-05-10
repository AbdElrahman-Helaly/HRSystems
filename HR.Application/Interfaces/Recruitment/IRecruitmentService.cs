using internalEmployee.Auth.Contracts;
using internalEmployee.Data.Entities;

namespace internalEmployee.Services.Recruitment;

public interface IRecruitmentService
{
    Task<RecruitmentRequestResponse> CreateRecruitmentRequestAsync(Guid currentUserId, CreateRecruitmentRequest request, CancellationToken ct);
    Task<PaginatedResponse<RecruitmentRequestResponse>> GetMyRecruitmentRequestsAsync(
        Guid currentUserId,
        string? search,
        RequestStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken ct);
    Task<PaginatedResponse<RecruitmentRequestResponse>> GetHrRecruitmentRequestsAsync(
        Guid currentUserId,
        string? search,
        RequestStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken ct);
    Task<RecruitmentRequestResponse?> GetRecruitmentRequestByIdAsync(int requestId, Guid currentUserId, CancellationToken ct);
    Task<RecruitmentRequestResponse> UpdateRecruitmentRequestStatusAsync(
        int requestId,
        Guid currentUserId,
        UpdateRecruitmentRequestStatusRequest request,
        CancellationToken ct);
    Task<RecruitmentCandidateResponse> CreateRecruitmentCandidateAsync(
        int requestId,
        Guid currentUserId,
        CreateRecruitmentCandidateRequest request,
        CancellationToken ct);
    Task<PaginatedResponse<RecruitmentCandidateResponse>> GetRecruitmentCandidatesAsync(
        int requestId,
        Guid currentUserId,
        string? search,
        RequestStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken ct);
    Task<RecruitmentCandidateResponse?> GetRecruitmentCandidateByIdAsync(int candidateId, Guid currentUserId, CancellationToken ct);
    Task<RecruitmentCandidateFileResponse> GetRecruitmentCandidateCvAsync(int candidateId, Guid currentUserId, CancellationToken ct);
    Task<RecruitmentCandidateResponse> UpdateRecruitmentCandidateStatusAsync(
        int candidateId,
        Guid currentUserId,
        UpdateRecruitmentCandidateStatusRequest request,
        CancellationToken ct);
    Task<bool> DeleteRecruitmentCandidateAsync(int candidateId, Guid currentUserId, CancellationToken ct);
}
