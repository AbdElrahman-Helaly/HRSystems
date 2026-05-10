using internalEmployee.Auth.Contracts;
using internalEmployee.Auth.Models;
using internalEmployee.Data;
using internalEmployee.Data.Entities;
using internalEmployee.Services.Notification;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace internalEmployee.Services.Recruitment;

public sealed class RecruitmentService : IRecruitmentService
{
    private sealed record RecruitmentCandidateRow(
        int Id,
        int RecruitmentRequestId,
        string FullName,
        string? PhoneNumber,
        string? Email,
        int? ExperienceYears,
        string? Notes,
        string CvOriginalFileName,
        string CvFilePath,
        RequestStatus Status,
        string? ManagerResponseNote,
        Guid SubmittedByHrUserId,
        string? FirstNameAr,
        string? MiddleNameAr,
        string? LastNameAr,
        string? FirstNameEn,
        string? MiddleNameEn,
        string? LastNameEn,
        DateTime CreatedAt,
        DateTime? UpdatedAt);

    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IWebHostEnvironment _environment;
    private const long MaxCvFileSize = 10 * 1024 * 1024;
    private static readonly string[] AllowedCvExtensions = { ".pdf" };

    public RecruitmentService(
        AppDbContext db,
        INotificationService notificationService,
        IWebHostEnvironment environment)
    {
        _db = db;
        _notificationService = notificationService;
        _environment = environment;
    }

    public async Task<RecruitmentRequestResponse> CreateRecruitmentRequestAsync(Guid currentUserId, CreateRecruitmentRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var currentUser = await GetCurrentUserAsync(currentUserId, ct);
        if (currentUser.Role != AppRole.Admin)
            throw new InvalidOperationException("Only department manager can create a recruitment request.");

        if (!currentUser.DepartmentId.HasValue)
            throw new InvalidOperationException("Manager must belong to a department before creating a recruitment request.");

        var department = await _db.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == currentUser.DepartmentId.Value, ct);

        if (department == null)
            throw new InvalidOperationException("Manager department not found.");

        var entity = new RecruitmentRequest
        {
            RequestedByUserId = currentUserId,
            DepartmentId = currentUser.DepartmentId.Value,
            RequestedJobTitle = NormalizeRequiredText(request.RequestedJobTitle, "Requested job title is required."),
            RequiredCount = request.RequiredCount,
            RequiredExperienceYears = request.RequiredExperienceYears,
            Skills = NormalizeOptionalText(request.Skills),
            Description = NormalizeOptionalText(request.Description),
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.Now
        };

        _db.RecruitmentRequests.Add(entity);
        await _db.SaveChangesAsync(ct);

        var hrRecipients = await _db.Users
            .AsNoTracking()
            .Where(u => u.Role == AppRole.HR && u.IsActive)
            .Select(u => u.Id)
            .Distinct()
            .ToListAsync(ct);

        if (hrRecipients.Count > 0)
        {
            var requesterName = BuildFullName(currentUser.FirstNameAr, currentUser.MiddleNameAr, currentUser.LastNameAr)
                ?? BuildFullName(currentUser.FirstNameEn, currentUser.MiddleNameEn, currentUser.LastNameEn)
                ?? currentUser.EmployeeCode
                ?? currentUser.PhoneNumber
                ?? "مدير القسم";

            var message =
                $"طلب احتياج توظيف جديد من {requesterName} لقسم {department.Name}. " +
                $"الوظيفة المطلوبة: {entity.RequestedJobTitle}. العدد المطلوب: {entity.RequiredCount}." +
                (entity.RequiredExperienceYears.HasValue ? $" الخبرة المطلوبة: {entity.RequiredExperienceYears.Value} سنة." : string.Empty) +
                (string.IsNullOrWhiteSpace(entity.Skills) ? string.Empty : $" المهارات المطلوبة: {entity.Skills}.");

            await _notificationService.SendCustomNotificationAsync(
                NotificationType.RecruitmentRequest,
                entity.Id,
                "طلب احتياج توظيف جديد",
                message,
                hrRecipients,
                new Dictionary<string, string>
                {
                    ["recruitmentRequestId"] = entity.Id.ToString()
                },
                ct);
        }

        return await GetRequiredRecruitmentRequestResponseAsync(entity.Id, currentUserId, ct);
    }

    public async Task<PaginatedResponse<RecruitmentRequestResponse>> GetMyRecruitmentRequestsAsync(
        Guid currentUserId,
        string? search,
        RequestStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var currentUser = await GetCurrentUserAsync(currentUserId, ct);
        if (currentUser.Role != AppRole.Admin && currentUser.Role != AppRole.SuperAdmin)
            throw new InvalidOperationException("Only department manager can access their recruitment requests.");

        return await BuildRecruitmentRequestsPageAsync(
            query: _db.RecruitmentRequests.AsNoTracking().Where(x => x.RequestedByUserId == currentUserId),
            search,
            status,
            pageNumber,
            pageSize,
            ct);
    }

    public async Task<PaginatedResponse<RecruitmentRequestResponse>> GetHrRecruitmentRequestsAsync(
        Guid currentUserId,
        string? search,
        RequestStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var currentUser = await GetCurrentUserAsync(currentUserId, ct);
        if (currentUser.Role != AppRole.HR && currentUser.Role != AppRole.SuperAdmin)
            throw new InvalidOperationException("Only HR or SuperAdmin can access recruitment requests inbox.");

        return await BuildRecruitmentRequestsPageAsync(
            query: _db.RecruitmentRequests.AsNoTracking(),
            search,
            status,
            pageNumber,
            pageSize,
            ct);
    }

    public async Task<RecruitmentRequestResponse?> GetRecruitmentRequestByIdAsync(int requestId, Guid currentUserId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var entity = await _db.RecruitmentRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == requestId, ct);

        if (entity == null)
            return null;

        var currentUser = await GetCurrentUserAsync(currentUserId, ct);
        EnsureCanAccessRecruitmentRequest(currentUser, entity);

        return await MapRecruitmentRequestAsync(entity, ct);
    }

    public async Task<RecruitmentRequestResponse> UpdateRecruitmentRequestStatusAsync(
        int requestId,
        Guid currentUserId,
        UpdateRecruitmentRequestStatusRequest request,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var currentUser = await GetCurrentUserAsync(currentUserId, ct);
        if (currentUser.Role != AppRole.HR && currentUser.Role != AppRole.SuperAdmin)
            throw new InvalidOperationException("Only HR or SuperAdmin can update recruitment request status.");

        var entity = await _db.RecruitmentRequests
            .FirstOrDefaultAsync(x => x.Id == requestId, ct);

        if (entity == null)
            throw new KeyNotFoundException("Recruitment request not found.");

        if (request.Status == RequestStatus.Rejected && string.IsNullOrWhiteSpace(request.Note))
            throw new InvalidOperationException("Rejection note is required when rejecting the recruitment request.");

        entity.Status = request.Status;
        entity.HrResponseNote = NormalizeOptionalText(request.Note);
        entity.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync(ct);

        await _notificationService.SendStatusChangeNotificationAsync(
            entity.RequestedByUserId,
            NotificationType.RecruitmentRequest,
            entity.Id,
            entity.Status,
            entity.HrResponseNote,
            ct);

        return await GetRequiredRecruitmentRequestResponseAsync(entity.Id, currentUserId, ct);
    }

    public async Task<RecruitmentCandidateResponse> CreateRecruitmentCandidateAsync(
        int requestId,
        Guid currentUserId,
        CreateRecruitmentCandidateRequest request,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var currentUser = await GetCurrentUserAsync(currentUserId, ct);
        if (currentUser.Role != AppRole.HR && currentUser.Role != AppRole.SuperAdmin)
            throw new InvalidOperationException("Only HR or SuperAdmin can submit candidates.");

        var recruitmentRequest = await _db.RecruitmentRequests
            .FirstOrDefaultAsync(x => x.Id == requestId, ct);

        if (recruitmentRequest == null)
            throw new KeyNotFoundException("Recruitment request not found.");

        if (recruitmentRequest.Status == RequestStatus.Rejected)
            throw new InvalidOperationException("Cannot submit candidate for a rejected recruitment request.");

        ValidateCvFile(request.CvFile);

        var uploadsFolder = Path.Combine(GetOrCreateWebRootPath(), "uploads", "recruitment-cvs", $"request-{requestId}");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var originalFileName = Path.GetFileName(request.CvFile.FileName);
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(originalFileName)}{extension}";
        var physicalFilePath = Path.Combine(uploadsFolder, storedFileName);

        await using (var stream = new FileStream(physicalFilePath, FileMode.Create))
        {
            await request.CvFile.CopyToAsync(stream, ct);
        }

        var candidate = new RecruitmentCandidate
        {
            RecruitmentRequestId = requestId,
            FullName = NormalizeRequiredText(request.FullName, "Candidate full name is required."),
            PhoneNumber = NormalizeOptionalText(request.PhoneNumber),
            Email = NormalizeOptionalText(request.Email),
            ExperienceYears = request.ExperienceYears,
            Notes = NormalizeOptionalText(request.Notes),
            CvFileName = storedFileName,
            CvOriginalFileName = originalFileName,
            CvFilePath = $"/uploads/recruitment-cvs/request-{requestId}/{storedFileName}",
            CvContentType = string.IsNullOrWhiteSpace(request.CvFile.ContentType) ? "application/pdf" : request.CvFile.ContentType,
            CvFileSize = request.CvFile.Length,
            SubmittedByHrUserId = currentUserId,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.Now
        };

        if (recruitmentRequest.Status == RequestStatus.Pending)
        {
            recruitmentRequest.Status = RequestStatus.Approved;
            recruitmentRequest.UpdatedAt = DateTime.Now;
        }

        _db.RecruitmentCandidates.Add(candidate);
        await _db.SaveChangesAsync(ct);

        var message =
            $"تم إرسال مرشح جديد لوظيفة {recruitmentRequest.RequestedJobTitle}. " +
            $"اسم المرشح: {candidate.FullName}. " +
            (candidate.ExperienceYears.HasValue ? $"الخبرة: {candidate.ExperienceYears.Value} سنة." : string.Empty);

        await _notificationService.SendCustomNotificationAsync(
            NotificationType.RecruitmentCandidate,
            candidate.Id,
            "مرشح جديد",
            message,
            new[] { recruitmentRequest.RequestedByUserId },
            new Dictionary<string, string>
            {
                ["candidateId"] = candidate.Id.ToString(),
                ["recruitmentRequestId"] = recruitmentRequest.Id.ToString(),
                ["cvUrl"] = candidate.CvFilePath
            },
            ct);

        return await GetRequiredRecruitmentCandidateResponseAsync(candidate.Id, currentUserId, ct);
    }

    public async Task<PaginatedResponse<RecruitmentCandidateResponse>> GetRecruitmentCandidatesAsync(
        int requestId,
        Guid currentUserId,
        string? search,
        RequestStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var recruitmentRequest = await _db.RecruitmentRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == requestId, ct);

        if (recruitmentRequest == null)
            throw new KeyNotFoundException("Recruitment request not found.");

        var currentUser = await GetCurrentUserAsync(currentUserId, ct);
        EnsureCanAccessRecruitmentRequest(currentUser, recruitmentRequest);

        if (pageNumber <= 0)
            pageNumber = 1;

        if (pageSize <= 0)
            pageSize = 10;

        if (pageSize > 100)
            pageSize = 100;

        var query =
            from candidate in _db.RecruitmentCandidates.AsNoTracking()
            join hrUser in _db.Users.AsNoTracking() on candidate.SubmittedByHrUserId equals hrUser.Id
            where candidate.RecruitmentRequestId == requestId
            select new RecruitmentCandidateRow(
                candidate.Id,
                candidate.RecruitmentRequestId,
                candidate.FullName,
                candidate.PhoneNumber,
                candidate.Email,
                candidate.ExperienceYears,
                candidate.Notes,
                candidate.CvOriginalFileName,
                candidate.CvFilePath,
                candidate.Status,
                candidate.ManagerResponseNote,
                candidate.SubmittedByHrUserId,
                hrUser.FirstNameAr,
                hrUser.MiddleNameAr,
                hrUser.LastNameAr,
                hrUser.FirstNameEn,
                hrUser.MiddleNameEn,
                hrUser.LastNameEn,
                candidate.CreatedAt,
                candidate.UpdatedAt);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        var normalizedSearch = NormalizeOptionalText(search);
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var pattern = $"%{normalizedSearch}%";
            query = query.Where(x =>
                EF.Functions.Like(x.FullName, pattern) ||
                (x.PhoneNumber != null && EF.Functions.Like(x.PhoneNumber, pattern)) ||
                (x.Email != null && EF.Functions.Like(x.Email, pattern)) ||
                (x.Notes != null && EF.Functions.Like(x.Notes, pattern)) ||
                EF.Functions.Like(x.CvOriginalFileName, pattern));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResponse<RecruitmentCandidateResponse>
        {
            Items = items.Select(MapRecruitmentCandidate).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<RecruitmentCandidateResponse?> GetRecruitmentCandidateByIdAsync(int candidateId, Guid currentUserId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var candidate = await _db.RecruitmentCandidates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == candidateId, ct);

        if (candidate == null)
            return null;

        var recruitmentRequest = await _db.RecruitmentRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == candidate.RecruitmentRequestId, ct);

        if (recruitmentRequest == null)
            throw new InvalidOperationException("Recruitment request not found.");

        var currentUser = await GetCurrentUserAsync(currentUserId, ct);
        EnsureCanAccessRecruitmentRequest(currentUser, recruitmentRequest);

        return await MapRecruitmentCandidateByIdAsync(candidateId, ct);
    }

    public async Task<RecruitmentCandidateFileResponse> GetRecruitmentCandidateCvAsync(int candidateId, Guid currentUserId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var candidate = await _db.RecruitmentCandidates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == candidateId, ct);

        if (candidate == null)
            throw new KeyNotFoundException("Recruitment candidate not found.");

        var recruitmentRequest = await _db.RecruitmentRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == candidate.RecruitmentRequestId, ct);

        if (recruitmentRequest == null)
            throw new InvalidOperationException("Recruitment request not found.");

        var currentUser = await GetCurrentUserAsync(currentUserId, ct);
        EnsureCanAccessRecruitmentRequest(currentUser, recruitmentRequest);

        var physicalPath = Path.Combine(GetOrCreateWebRootPath(), candidate.CvFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(physicalPath))
            throw new FileNotFoundException("CV file not found.");

        return new RecruitmentCandidateFileResponse
        {
            FilePath = physicalPath,
            ContentType = candidate.CvContentType,
            FileName = candidate.CvOriginalFileName
        };
    }

    public async Task<RecruitmentCandidateResponse> UpdateRecruitmentCandidateStatusAsync(
        int candidateId,
        Guid currentUserId,
        UpdateRecruitmentCandidateStatusRequest request,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var currentUser = await GetCurrentUserAsync(currentUserId, ct);

        var candidate = await _db.RecruitmentCandidates
            .FirstOrDefaultAsync(x => x.Id == candidateId, ct);

        if (candidate == null)
            throw new KeyNotFoundException("Recruitment candidate not found.");

        var recruitmentRequest = await _db.RecruitmentRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == candidate.RecruitmentRequestId, ct);

        if (recruitmentRequest == null)
            throw new InvalidOperationException("Recruitment request not found.");

        EnsureCanManageRecruitmentCandidate(currentUser, recruitmentRequest);

        if (request.Status == RequestStatus.Rejected && string.IsNullOrWhiteSpace(request.ResponseNote))
            throw new InvalidOperationException("Response note is required when rejecting a candidate.");

        candidate.Status = request.Status;
        candidate.ManagerResponseNote = NormalizeOptionalText(request.ResponseNote);
        candidate.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync(ct);

        await _notificationService.SendStatusChangeNotificationAsync(
            candidate.SubmittedByHrUserId,
            NotificationType.RecruitmentCandidate,
            candidate.Id,
            candidate.Status,
            candidate.ManagerResponseNote,
            ct);

        return await GetRequiredRecruitmentCandidateResponseAsync(candidate.Id, currentUserId, ct);
    }

    public async Task<bool> DeleteRecruitmentCandidateAsync(int candidateId, Guid currentUserId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var currentUser = await GetCurrentUserAsync(currentUserId, ct);
        if (currentUser.Role != AppRole.HR && currentUser.Role != AppRole.SuperAdmin)
            throw new InvalidOperationException("Only HR or SuperAdmin can delete recruitment candidates.");

        var candidate = await _db.RecruitmentCandidates
            .FirstOrDefaultAsync(x => x.Id == candidateId, ct);

        if (candidate == null)
            return false;

        var physicalPath = Path.Combine(GetOrCreateWebRootPath(), candidate.CvFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(physicalPath))
        {
            try
            {
                File.Delete(physicalPath);
            }
            catch
            {
            }
        }

        _db.RecruitmentCandidates.Remove(candidate);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<PaginatedResponse<RecruitmentRequestResponse>> BuildRecruitmentRequestsPageAsync(
        IQueryable<RecruitmentRequest> query,
        string? search,
        RequestStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken ct)
    {
        if (pageNumber <= 0)
            pageNumber = 1;

        if (pageSize <= 0)
            pageSize = 10;

        if (pageSize > 100)
            pageSize = 100;

        var projectedQuery =
            from recruitmentRequest in query
            join requester in _db.Users.AsNoTracking() on recruitmentRequest.RequestedByUserId equals requester.Id
            join department in _db.Departments.AsNoTracking() on recruitmentRequest.DepartmentId equals department.Id into departments
            from department in departments.DefaultIfEmpty()
            select new
            {
                recruitmentRequest.Id,
                recruitmentRequest.RequestedByUserId,
                requester.FirstNameAr,
                requester.MiddleNameAr,
                requester.LastNameAr,
                requester.FirstNameEn,
                requester.MiddleNameEn,
                requester.LastNameEn,
                recruitmentRequest.DepartmentId,
                DepartmentName = department != null ? department.Name : null,
                recruitmentRequest.RequestedJobTitle,
                recruitmentRequest.RequiredCount,
                recruitmentRequest.RequiredExperienceYears,
                recruitmentRequest.Skills,
                recruitmentRequest.Description,
                recruitmentRequest.Status,
                recruitmentRequest.HrResponseNote,
                CandidatesCount = _db.RecruitmentCandidates.Count(c => c.RecruitmentRequestId == recruitmentRequest.Id),
                recruitmentRequest.CreatedAt,
                recruitmentRequest.UpdatedAt
            };

        if (status.HasValue)
            projectedQuery = projectedQuery.Where(x => x.Status == status.Value);

        var normalizedSearch = NormalizeOptionalText(search);
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var pattern = $"%{normalizedSearch}%";
            projectedQuery = projectedQuery.Where(x =>
                EF.Functions.Like(x.RequestedJobTitle, pattern) ||
                (x.Skills != null && EF.Functions.Like(x.Skills, pattern)) ||
                (x.Description != null && EF.Functions.Like(x.Description, pattern)) ||
                (x.DepartmentName != null && EF.Functions.Like(x.DepartmentName, pattern)) ||
                (x.FirstNameAr != null && EF.Functions.Like(x.FirstNameAr, pattern)) ||
                (x.MiddleNameAr != null && EF.Functions.Like(x.MiddleNameAr, pattern)) ||
                (x.LastNameAr != null && EF.Functions.Like(x.LastNameAr, pattern)) ||
                (x.FirstNameEn != null && EF.Functions.Like(x.FirstNameEn, pattern)) ||
                (x.MiddleNameEn != null && EF.Functions.Like(x.MiddleNameEn, pattern)) ||
                (x.LastNameEn != null && EF.Functions.Like(x.LastNameEn, pattern)));
        }

        var totalCount = await projectedQuery.CountAsync(ct);

        var rows = await projectedQuery
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResponse<RecruitmentRequestResponse>
        {
            Items = rows.Select(x => new RecruitmentRequestResponse
            {
                Id = x.Id,
                RequestedByUserId = x.RequestedByUserId,
                RequesterFullNameAr = BuildFullName(x.FirstNameAr, x.MiddleNameAr, x.LastNameAr),
                RequesterFullNameEn = BuildFullName(x.FirstNameEn, x.MiddleNameEn, x.LastNameEn),
                DepartmentId = x.DepartmentId,
                DepartmentName = x.DepartmentName,
                RequestedJobTitle = x.RequestedJobTitle,
                RequiredCount = x.RequiredCount,
                RequiredExperienceYears = x.RequiredExperienceYears,
                Skills = x.Skills,
                Description = x.Description,
                Status = x.Status.ToString(),
                HrResponseNote = x.HrResponseNote,
                CandidatesCount = x.CandidatesCount,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            }).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private async Task<RecruitmentRequestResponse> GetRequiredRecruitmentRequestResponseAsync(int requestId, Guid currentUserId, CancellationToken ct)
    {
        var response = await GetRecruitmentRequestByIdAsync(requestId, currentUserId, ct);
        if (response == null)
            throw new InvalidOperationException("Recruitment request not found.");

        return response;
    }

    private async Task<RecruitmentCandidateResponse> GetRequiredRecruitmentCandidateResponseAsync(int candidateId, Guid currentUserId, CancellationToken ct)
    {
        var response = await GetRecruitmentCandidateByIdAsync(candidateId, currentUserId, ct);
        if (response == null)
            throw new InvalidOperationException("Recruitment candidate not found.");

        return response;
    }

    private async Task<RecruitmentRequestResponse> MapRecruitmentRequestAsync(RecruitmentRequest entity, CancellationToken ct)
    {
        var requester = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == entity.RequestedByUserId, ct);

        var department = entity.DepartmentId.HasValue
            ? await _db.Departments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.DepartmentId.Value, ct)
            : null;

        var candidatesCount = await _db.RecruitmentCandidates
            .AsNoTracking()
            .CountAsync(x => x.RecruitmentRequestId == entity.Id, ct);

        return new RecruitmentRequestResponse
        {
            Id = entity.Id,
            RequestedByUserId = entity.RequestedByUserId,
            RequesterFullNameAr = requester == null ? null : BuildFullName(requester.FirstNameAr, requester.MiddleNameAr, requester.LastNameAr),
            RequesterFullNameEn = requester == null ? null : BuildFullName(requester.FirstNameEn, requester.MiddleNameEn, requester.LastNameEn),
            DepartmentId = entity.DepartmentId,
            DepartmentName = department?.Name,
            RequestedJobTitle = entity.RequestedJobTitle,
            RequiredCount = entity.RequiredCount,
            RequiredExperienceYears = entity.RequiredExperienceYears,
            Skills = entity.Skills,
            Description = entity.Description,
            Status = entity.Status.ToString(),
            HrResponseNote = entity.HrResponseNote,
            CandidatesCount = candidatesCount,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private async Task<RecruitmentCandidateResponse> MapRecruitmentCandidateByIdAsync(int candidateId, CancellationToken ct)
    {
        var row = await (
            from candidate in _db.RecruitmentCandidates.AsNoTracking()
            join hrUser in _db.Users.AsNoTracking() on candidate.SubmittedByHrUserId equals hrUser.Id
            where candidate.Id == candidateId
            select new RecruitmentCandidateRow(
                candidate.Id,
                candidate.RecruitmentRequestId,
                candidate.FullName,
                candidate.PhoneNumber,
                candidate.Email,
                candidate.ExperienceYears,
                candidate.Notes,
                candidate.CvOriginalFileName,
                candidate.CvFilePath,
                candidate.Status,
                candidate.ManagerResponseNote,
                candidate.SubmittedByHrUserId,
                hrUser.FirstNameAr,
                hrUser.MiddleNameAr,
                hrUser.LastNameAr,
                hrUser.FirstNameEn,
                hrUser.MiddleNameEn,
                hrUser.LastNameEn,
                candidate.CreatedAt,
                candidate.UpdatedAt)).FirstOrDefaultAsync(ct);

        if (row == null)
            throw new InvalidOperationException("Recruitment candidate not found.");

        return MapRecruitmentCandidate(row);
    }

    private static RecruitmentCandidateResponse MapRecruitmentCandidate(RecruitmentCandidateRow row) =>
        new()
        {
            Id = row.Id,
            RecruitmentRequestId = row.RecruitmentRequestId,
            FullName = row.FullName,
            PhoneNumber = row.PhoneNumber,
            Email = row.Email,
            ExperienceYears = row.ExperienceYears,
            Notes = row.Notes,
            CvOriginalFileName = row.CvOriginalFileName,
            CvUrl = row.CvFilePath,
            Status = row.Status.ToString(),
            ManagerResponseNote = row.ManagerResponseNote,
            SubmittedByHrUserId = row.SubmittedByHrUserId,
            SubmittedByHrNameAr = BuildFullName(row.FirstNameAr, row.MiddleNameAr, row.LastNameAr),
            SubmittedByHrNameEn = BuildFullName(row.FirstNameEn, row.MiddleNameEn, row.LastNameEn),
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt
        };

    private string GetOrCreateWebRootPath()
    {
        var webRootPath = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
            webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");

        if (!Directory.Exists(webRootPath))
            Directory.CreateDirectory(webRootPath);

        return webRootPath;
    }

    private static void ValidateCvFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("CV PDF file is required.");

        if (file.Length > MaxCvFileSize)
            throw new InvalidOperationException($"CV file exceeds maximum size of {MaxCvFileSize / (1024 * 1024)} MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedCvExtensions.Contains(extension))
            throw new InvalidOperationException("Only PDF files are allowed for CV upload.");
    }

    private void EnsureCanAccessRecruitmentRequest(AppUser currentUser, RecruitmentRequest recruitmentRequest)
    {
        if (currentUser.Role == AppRole.SuperAdmin || currentUser.Role == AppRole.HR)
            return;

        if (currentUser.Role == AppRole.Admin && recruitmentRequest.RequestedByUserId == currentUser.Id)
            return;

        throw new InvalidOperationException("You are not allowed to access this recruitment request.");
    }

    private static void EnsureCanManageRecruitmentCandidate(AppUser currentUser, RecruitmentRequest recruitmentRequest)
    {
        if (currentUser.Role == AppRole.SuperAdmin)
            return;

        if (currentUser.Role == AppRole.Admin && recruitmentRequest.RequestedByUserId == currentUser.Id)
            return;

        throw new InvalidOperationException("Only the request manager can update candidate status.");
    }

    private async Task<AppUser> GetCurrentUserAsync(Guid currentUserId, CancellationToken ct)
    {
        var currentUser = await _db.Users.FirstOrDefaultAsync(x => x.Id == currentUserId, ct);
        if (currentUser == null)
            throw new InvalidOperationException("Current user not found.");

        if (!currentUser.IsActive)
            throw new InvalidOperationException("Current user is inactive.");

        return currentUser;
    }

    private static string NormalizeRequiredText(string? value, string errorMessage)
    {
        var normalized = NormalizeOptionalText(value);
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException(errorMessage);

        return normalized;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? BuildFullName(string? firstName, string? middleName, string? lastName)
    {
        var fullName = string.Join(" ", new[] { firstName, middleName, lastName }
            .Where(x => !string.IsNullOrWhiteSpace(x)));

        return string.IsNullOrWhiteSpace(fullName) ? null : fullName;
    }
}
