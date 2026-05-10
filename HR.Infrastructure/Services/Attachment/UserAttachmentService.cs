using internalEmployee.Auth.Models;
using internalEmployee.Data;
using internalEmployee.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

namespace internalEmployee.Services.Attachment;

public sealed class UserAttachmentService : IUserAttachmentService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
    private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".xls", ".xlsx" };

    public UserAttachmentService(AppDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    public async Task<List<UserAttachment>> UploadAttachmentsAsync(Guid userId, List<IFormFile> files, CancellationToken ct)
    {
        if (files == null || files.Count == 0)
            throw new InvalidOperationException("No files provided.");

        // Verify user exists
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        // Determine folder name
        var folderName = GetUserFolderName(user);
        var userFolderPath = Path.Combine(_environment.WebRootPath, "users", folderName, "files");
        
        // Create directory if it doesn't exist
        if (!Directory.Exists(userFolderPath))
            Directory.CreateDirectory(userFolderPath);

        var uploadedAttachments = new List<UserAttachment>();

        foreach (var file in files)
        {
            if (file == null || file.Length == 0)
                continue;

            // Validate file size
            if (file.Length > MaxFileSize)
                throw new InvalidOperationException($"File {file.FileName} exceeds maximum size of {MaxFileSize / (1024 * 1024)} MB.");

            // Validate file extension
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(fileExtension))
                throw new InvalidOperationException($"File {file.FileName} has an invalid extension. Allowed extensions: {string.Join(", ", AllowedExtensions)}");

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(userFolderPath, uniqueFileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, ct);
            }

            // Create attachment record
            var attachment = new UserAttachment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FileName = uniqueFileName,
                OriginalFileName = file.FileName,
                FilePath = $"/users/{folderName}/files/{uniqueFileName}",
                FileSize = file.Length,
                ContentType = file.ContentType,
                UploadedAt = DateTime.Now
            };

            _db.UserAttachments.Add(attachment);
            uploadedAttachments.Add(attachment);
        }

        await _db.SaveChangesAsync(ct);
        return uploadedAttachments;
    }

    public async Task<List<UserAttachment>> GetUserAttachmentsAsync(Guid userId, CancellationToken ct)
    {
        return await _db.UserAttachments
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.UploadedAt)
            .ToListAsync(ct);
    }

    public async Task<bool> DeleteAttachmentAsync(Guid userId, Guid attachmentId, CancellationToken ct)
    {
        var attachment = await _db.UserAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.UserId == userId, ct);

        if (attachment == null)
            return false;

        // Delete physical file
        var filePath = Path.Combine(_environment.WebRootPath, attachment.FilePath.TrimStart('/'));
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
            }
            catch
            {
                // Log error but continue with database deletion
            }
        }

        // Delete database record
        _db.UserAttachments.Remove(attachment);
        await _db.SaveChangesAsync(ct);

        return true;
    }

    public async Task<bool> DeleteAttachmentByAdminAsync(Guid attachmentId, CancellationToken ct)
    {
        var attachment = await _db.UserAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId, ct);

        if (attachment == null)
            return false;

        // Delete physical file
        var filePath = Path.Combine(_environment.WebRootPath, attachment.FilePath.TrimStart('/'));
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
            }
            catch
            {
                // Log error but continue with database deletion
            }
        }

        // Delete database record
        _db.UserAttachments.Remove(attachment);
        await _db.SaveChangesAsync(ct);

        return true;
    }

    private static string GetUserFolderName(AppUser user)
    {
        // Use FirstNameEn_LastNameEn if available
        if (!string.IsNullOrWhiteSpace(user.FirstNameEn) && !string.IsNullOrWhiteSpace(user.LastNameEn))
            return $"{user.FirstNameEn}_{user.LastNameEn}";

        // Fallback to FirstNameAr_LastNameAr
        if (!string.IsNullOrWhiteSpace(user.FirstNameAr) && !string.IsNullOrWhiteSpace(user.LastNameAr))
            return $"{user.FirstNameAr}_{user.LastNameAr}";

        // Fallback to UserId
        return user.Id.ToString();
    }
}
