using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using internalEmployee.Auth.Contracts;

namespace internalEmployee.Services.Meeting;

public interface IMeetingService
{
    /// <summary>
    /// Creates a meeting and sends notifications to appropriate users.
    /// </summary>
    /// <param name="request">Details of the meeting to create.</param>
    /// <param name="creatorUserId">Id of the user (admin or superadmin) creating the meeting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created meeting id.</returns>
    Task<int> CreateMeetingAsync(MeetingCreateRequest request, System.Guid creatorUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing meeting.
    /// SuperAdmin can update any meeting, Admin can update only their own meetings.
    /// </summary>
    Task UpdateMeetingAsync(int meetingId, MeetingUpdateRequest request, System.Guid currentUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an existing meeting.
    /// SuperAdmin can delete any meeting, Admin can delete only their own meetings.
    /// </summary>
    Task DeleteMeetingAsync(int meetingId, System.Guid currentUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets meetings with pagination.
    /// SuperAdmin can view all meetings, Admin can view meetings they created.
    /// </summary>
    Task<PaginatedResponse<MeetingResponse>> GetMeetingsPaginatedAsync(
        int pageNumber,
        int pageSize,
        System.Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload attachments to an existing meeting.
    /// SuperAdmin can upload to any meeting, Admin can upload only to meetings they created.
    /// </summary>
    Task<List<MeetingAttachmentResponse>> UploadMeetingAttachmentsAsync(
        int meetingId,
        System.Guid currentUserId,
        List<Microsoft.AspNetCore.Http.IFormFile> files,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get attachments of a meeting.
    /// SuperAdmin can view any meeting attachments, Admin can view only meetings they created.
    /// </summary>
    Task<List<MeetingAttachmentResponse>> GetMeetingAttachmentsAsync(
        int meetingId,
        System.Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete one attachment from a meeting.
    /// SuperAdmin can delete from any meeting, Admin can delete only from meetings they created.
    /// </summary>
    Task DeleteMeetingAttachmentAsync(
        int meetingId,
        System.Guid attachmentId,
        System.Guid currentUserId,
        CancellationToken cancellationToken = default);
}
