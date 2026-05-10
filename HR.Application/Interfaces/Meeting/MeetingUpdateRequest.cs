using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Services.Meeting;

public sealed class MeetingUpdateRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    [Required]
    public DateOnly MeetingDate { get; set; }

    [Required]
    public TimeOnly MeetingTime { get; set; }
}
