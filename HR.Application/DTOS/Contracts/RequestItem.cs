namespace internalEmployee.Auth.Contracts;

public sealed class RequestItem
{
    public required int Id { get; init; }
    public required string Type { get; init; } // "Permission", "Leave", "Assignment", "Overtime", "Advance"
    public required DateTime CreatedAt { get; init; }
    public required string Status { get; init; }
    public Guid? UserId { get; init; }
    public DateOnly? Date { get; init; } // For Permission
    public DateOnly? StartDate { get; init; } // For Leave and Assignment
    public DateOnly? EndDate { get; init; } // For Leave
    public TimeOnly? StartTime { get; init; } // For Permission and Assignment
    public TimeOnly? EndTime { get; init; } // For Permission and Assignment
    public string? Where { get; init; } // For Assignment
    public string? Reason { get; init; }
    public decimal? Amount { get; init; } // For SalaryAdvance
    public decimal? MonthlyDeduction { get; init; } // For SalaryAdvance
}

