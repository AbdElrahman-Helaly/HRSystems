using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Services.Meeting;

public sealed class MeetingCreateRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    [Required]
    public DateOnly MeetingDate { get; set; }

    [Required]
    public TimeOnly MeetingTime { get; set; }

    // For SuperAdmin: specify department IDs to target. If null or empty, targets whole company.
    public List<int>? DepartmentIds { get; set; }
}
