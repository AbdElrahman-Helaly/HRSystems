using internalEmployee.Data.Entities;
using internalEmployee.Services.Attachment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace internalEmployee.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AttachmentController : ControllerBase
{
    private readonly IUserAttachmentService _attachmentService;

    public AttachmentController(IUserAttachmentService attachmentService)
    {
        _attachmentService = attachmentService;
    }

    [HttpPost("upload")]
    [ProducesResponseType(typeof(List<UserAttachment>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<UserAttachment>>> UploadAttachments(
        [FromForm] List<IFormFile> files,
        CancellationToken ct)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var result = await _attachmentService.UploadAttachmentsAsync(userId, files, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }

    [HttpGet("my-attachments")]
    [ProducesResponseType(typeof(List<UserAttachment>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserAttachment>>> GetMyAttachments(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await _attachmentService.GetUserAttachmentsAsync(userId, ct);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAttachment(Guid id, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var deleted = await _attachmentService.DeleteAttachmentAsync(userId, id, ct);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}

