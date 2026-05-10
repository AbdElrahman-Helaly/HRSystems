using System.ComponentModel.DataAnnotations;
using internalEmployee.Data.Entities;
using Microsoft.AspNetCore.Http;

namespace internalEmployee.Auth.Contracts;

public sealed class LeaveRequest
{
    [Required]
    public DateOnly StartDate { get; init; }

    [Required]
    public DateOnly EndDate { get; init; }

    public string? Reason { get; init; }

    [Required]
    public LeaveType LeaveType { get; init; } = LeaveType.Annual;

    public IFormFile? MedicalReport { get; init; }
}

