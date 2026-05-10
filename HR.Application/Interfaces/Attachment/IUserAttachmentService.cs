using Microsoft.AspNetCore.Http;
using internalEmployee.Data.Entities;

namespace internalEmployee.Services.Attachment;

public interface IUserAttachmentService
{
    Task<List<UserAttachment>> UploadAttachmentsAsync(Guid userId, List<IFormFile> files, CancellationToken ct);
    Task<List<UserAttachment>> GetUserAttachmentsAsync(Guid userId, CancellationToken ct);
    Task<bool> DeleteAttachmentAsync(Guid userId, Guid attachmentId, CancellationToken ct);
    Task<bool> DeleteAttachmentByAdminAsync(Guid attachmentId, CancellationToken ct);
}
