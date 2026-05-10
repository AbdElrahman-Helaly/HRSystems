using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using internalEmployee.Auth.Contracts;
using internalEmployee.Auth.Models;
using internalEmployee.Data;
using internalEmployee.Data.Entities;
using internalEmployee.Services.Notification;
using MeetingEntity = internalEmployee.Data.Entities.Meeting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace internalEmployee.Services.Meeting;

public sealed class MeetingService : IMeetingService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IWebHostEnvironment _environment;
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
    private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".xls", ".xlsx" };

    public MeetingService(AppDbContext db, INotificationService notificationService, IWebHostEnvironment environment)
    {
        _db = db;
        _notificationService = notificationService;
        _environment = environment;
    }

    public async Task<int> CreateMeetingAsync(MeetingCreateRequest request, Guid creatorUserId, CancellationToken cancellationToken = default)
    {
        // Get creator user to determine role and department
        var creator = await _db.Users.FirstOrDefaultAsync(u => u.Id == creatorUserId, cancellationToken);
        if (creator == null)
            throw new InvalidOperationException("Creator user not found.");

        var targetDepartmentIds = new List<int>();

        // Create meeting entity
        var meeting = new MeetingEntity
        {
            Title = request.Title,
            Message = request.Message,
            MeetingDate = request.MeetingDate,
            MeetingTime = request.MeetingTime,
            CreatedAt = DateTime.Now,
            CreatedByUserId = creatorUserId
        };
        _db.Meetings.Add(meeting);
        await _db.SaveChangesAsync(cancellationToken);

        // Determine recipients based on role
        List<Guid> recipientIds = new();
        if (creator.Role == AppRole.SuperAdmin)
        {
            // SuperAdmin: target specific departments or whole company
            if (request.DepartmentIds != null && request.DepartmentIds.Any())
            {
                targetDepartmentIds = request.DepartmentIds
                    .Where(x => x > 0)
                    .Distinct()
                    .ToList();

                if (targetDepartmentIds.Count == 0)
                    throw new InvalidOperationException("DepartmentIds must contain valid positive IDs.");

                var existingDepartmentIds = await _db.Departments
                    .Where(d => targetDepartmentIds.Contains(d.Id))
                    .Select(d => d.Id)
                    .ToListAsync(cancellationToken);

                var invalidDepartmentIds = targetDepartmentIds
                    .Except(existingDepartmentIds)
                    .ToList();

                if (invalidDepartmentIds.Count > 0)
                    throw new InvalidOperationException($"Invalid DepartmentIds: {string.Join(", ", invalidDepartmentIds)}");

                recipientIds = await _db.Users
                    .Where(u => u.IsActive && u.DepartmentId.HasValue && targetDepartmentIds.Contains(u.DepartmentId.Value))
                    .Select(u => u.Id)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                // Whole company (all active users)
                recipientIds = await _db.Users
                    .Where(u => u.IsActive)
                    .Select(u => u.Id)
                    .ToListAsync(cancellationToken);
            }
        }
        else if (creator.Role == AppRole.Admin)
        {
            // Admin: only users in the same department
            if (!creator.DepartmentId.HasValue)
                throw new InvalidOperationException("Admin does not belong to any department.");

            targetDepartmentIds.Add(creator.DepartmentId.Value);

            recipientIds = await _db.Users
                .Where(u => u.IsActive && u.DepartmentId == creator.DepartmentId)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);
        }
        else
        {
            throw new InvalidOperationException("Only Admin or SuperAdmin can create meetings.");
        }

        if (targetDepartmentIds.Count > 0)
        {
            var meetingDepartments = targetDepartmentIds
                .Select(deptId => new MeetingDepartment
                {
                    MeetingId = meeting.Id,
                    DepartmentId = deptId
                })
                .ToList();

            _db.MeetingDepartments.AddRange(meetingDepartments);
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Send notifications
        await _notificationService.SendMeetingNotificationAsync(meeting.Id, request.Title, request.Message, recipientIds, cancellationToken);

        return meeting.Id;
    }

    public async Task UpdateMeetingAsync(int meetingId, MeetingUpdateRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);
        if (currentUser == null)
            throw new InvalidOperationException("Current user not found.");

        var meeting = await _db.Meetings.FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken);
        if (meeting == null)
            throw new KeyNotFoundException("Meeting not found.");

        if (currentUser.Role == AppRole.Admin && meeting.CreatedByUserId != currentUserId)
            throw new InvalidOperationException("Admin can update only meetings they created.");

        if (currentUser.Role != AppRole.Admin && currentUser.Role != AppRole.SuperAdmin)
            throw new InvalidOperationException("Only Admin or SuperAdmin can update meetings.");

        meeting.Title = request.Title;
        meeting.Message = request.Message;
        meeting.MeetingDate = request.MeetingDate;
        meeting.MeetingTime = request.MeetingTime;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteMeetingAsync(int meetingId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var meeting = await EnsureCanManageMeetingAsync(meetingId, currentUserId, cancellationToken);

        var meetingFolderPath = Path.Combine(_environment.WebRootPath, "meetings", meetingId.ToString(), "files");
        if (Directory.Exists(meetingFolderPath))
        {
            try
            {
                Directory.Delete(meetingFolderPath, recursive: true);
            }
            catch
            {
                // Ignore filesystem cleanup errors and continue with DB deletion.
            }
        }

        _db.Meetings.Remove(meeting);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PaginatedResponse<MeetingResponse>> GetMeetingsPaginatedAsync(
        int pageNumber,
        int pageSize,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);
        if (currentUser == null)
            throw new InvalidOperationException("Current user not found.");

        if (currentUser.Role != AppRole.Admin && currentUser.Role != AppRole.SuperAdmin)
            throw new InvalidOperationException("Only Admin or SuperAdmin can view meetings.");

        var query = _db.Meetings.AsQueryable();

        if (currentUser.Role == AppRole.Admin)
            query = query.Where(m => m.CreatedByUserId == currentUserId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                Id = m.Id,
                Title = m.Title,
                Message = m.Message,
                MeetingDate = m.MeetingDate,
                MeetingTime = m.MeetingTime,
                CreatedAt = m.CreatedAt,
                CreatedByUserId = m.CreatedByUserId
            })
            .ToListAsync(cancellationToken);

        var meetingIds = items.Select(x => x.Id).ToList();
        var meetingDepartmentRows = await (
            from md in _db.MeetingDepartments
            join d in _db.Departments on md.DepartmentId equals d.Id
            where meetingIds.Contains(md.MeetingId)
            select new
            {
                md.MeetingId,
                DepartmentName = d.Name
            })
            .ToListAsync(cancellationToken);

        var departmentNamesByMeetingId = meetingDepartmentRows
            .GroupBy(x => x.MeetingId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.DepartmentName).Distinct().OrderBy(x => x).ToList());

        var mappedItems = items.Select(m =>
        {
            var hasDepartments = departmentNamesByMeetingId.TryGetValue(m.Id, out var names) && names != null && names.Count > 0;
            var targetDepartment = hasDepartments ? string.Join(", ", names!) : "All";

            return new MeetingResponse
            {
                Id = m.Id,
                Title = m.Title,
                Message = m.Message,
                MeetingDate = m.MeetingDate,
                MeetingTime = m.MeetingTime,
                CreatedAt = m.CreatedAt,
                CreatedByUserId = m.CreatedByUserId,
                TargetDepartment = targetDepartment
            };
        }).ToList();

        return new PaginatedResponse<MeetingResponse>
        {
            Items = mappedItems,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<List<MeetingAttachmentResponse>> UploadMeetingAttachmentsAsync(
        int meetingId,
        Guid currentUserId,
        List<IFormFile> files,
        CancellationToken cancellationToken = default)
    {
        if (files == null || files.Count == 0)
            throw new InvalidOperationException("No files provided.");

        await EnsureCanManageMeetingAsync(meetingId, currentUserId, cancellationToken);

        var meetingFolderPath = Path.Combine(_environment.WebRootPath, "meetings", meetingId.ToString(), "files");
        if (!Directory.Exists(meetingFolderPath))
            Directory.CreateDirectory(meetingFolderPath);

        var uploaded = new List<MeetingAttachment>();
        foreach (var file in files)
        {
            if (file == null || file.Length == 0)
                continue;

            if (file.Length > MaxFileSize)
                throw new InvalidOperationException($"File {file.FileName} exceeds maximum size of {MaxFileSize / (1024 * 1024)} MB.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                throw new InvalidOperationException($"File {file.FileName} has an invalid extension. Allowed extensions: {string.Join(", ", AllowedExtensions)}");

            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var physicalPath = Path.Combine(meetingFolderPath, uniqueFileName);
            await using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            var attachment = new MeetingAttachment
            {
                Id = Guid.NewGuid(),
                MeetingId = meetingId,
                UploadedByUserId = currentUserId,
                FileName = uniqueFileName,
                OriginalFileName = file.FileName,
                FilePath = $"/meetings/{meetingId}/files/{uniqueFileName}",
                FileSize = file.Length,
                ContentType = file.ContentType,
                UploadedAt = DateTime.Now
            };

            _db.MeetingAttachments.Add(attachment);
            uploaded.Add(attachment);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return uploaded
            .OrderByDescending(x => x.UploadedAt)
            .Select(x => new MeetingAttachmentResponse
            {
                Id = x.Id,
                MeetingId = x.MeetingId,
                OriginalFileName = x.OriginalFileName,
                FilePath = x.FilePath,
                ContentType = x.ContentType,
                FileSize = x.FileSize,
                UploadedAt = x.UploadedAt,
                UploadedByUserId = x.UploadedByUserId
            })
            .ToList();
    }

    public async Task<List<MeetingAttachmentResponse>> GetMeetingAttachmentsAsync(
        int meetingId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageMeetingAsync(meetingId, currentUserId, cancellationToken);

        var items = await _db.MeetingAttachments
            .Where(x => x.MeetingId == meetingId)
            .OrderByDescending(x => x.UploadedAt)
            .Select(x => new MeetingAttachmentResponse
            {
                Id = x.Id,
                MeetingId = x.MeetingId,
                OriginalFileName = x.OriginalFileName,
                FilePath = x.FilePath,
                ContentType = x.ContentType,
                FileSize = x.FileSize,
                UploadedAt = x.UploadedAt,
                UploadedByUserId = x.UploadedByUserId
            })
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task DeleteMeetingAttachmentAsync(
        int meetingId,
        Guid attachmentId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageMeetingAsync(meetingId, currentUserId, cancellationToken);

        var attachment = await _db.MeetingAttachments
            .FirstOrDefaultAsync(x => x.Id == attachmentId && x.MeetingId == meetingId, cancellationToken);
        if (attachment == null)
            throw new KeyNotFoundException("Attachment not found.");

        var physicalPath = Path.Combine(_environment.WebRootPath, attachment.FilePath.TrimStart('/'));
        if (File.Exists(physicalPath))
        {
            try
            {
                File.Delete(physicalPath);
            }
            catch
            {
                // Ignore filesystem deletion errors and continue with DB deletion.
            }
        }

        _db.MeetingAttachments.Remove(attachment);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<MeetingEntity> EnsureCanManageMeetingAsync(int meetingId, Guid currentUserId, CancellationToken cancellationToken)
    {
        var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);
        if (currentUser == null)
            throw new InvalidOperationException("Current user not found.");

        var meeting = await _db.Meetings.FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken);
        if (meeting == null)
            throw new KeyNotFoundException("Meeting not found.");

        if (currentUser.Role != AppRole.Admin && currentUser.Role != AppRole.SuperAdmin)
            throw new InvalidOperationException("Only Admin or SuperAdmin can manage meetings.");

        if (currentUser.Role == AppRole.Admin && meeting.CreatedByUserId != currentUserId)
            throw new InvalidOperationException("Admin can manage only meetings they created.");

        return meeting;
    }
}
