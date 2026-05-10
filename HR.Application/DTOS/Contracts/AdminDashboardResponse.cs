namespace internalEmployee.Auth.Contracts;

public sealed class AdminDashboardResponse
{
    public required string Greeting { get; init; }
    public string? FullNameAr { get; init; }
    public string? FullNameEn { get; init; }
    public string? JobTitle { get; init; }
    public string? DepartmentName { get; init; }
    public string? ImageUrl { get; init; }
    public TimeOnly? TodayAttendanceTime { get; init; }
    public TimeOnly? TodayDepartureTime { get; init; }
    public required List<RequestItem> AllRequests { get; init; }
    public required List<RequestItem> PendingRequests { get; init; }
    public required List<RequestItem> AcceptedRequests { get; init; }
    public required List<RequestItem> RejectedRequests { get; init; }
    public required List<EmployeeItem> Employees { get; init; }
}

// Backward-compatible type kept to avoid stale references during compilation.
public sealed class AdminUserItem
{
    public Guid Id { get; init; }
    public string? FirstNameAr { get; init; }
    public string? MiddleNameAr { get; init; }
    public string? LastNameAr { get; init; }
    public string? FirstNameEn { get; init; }
    public string? MiddleNameEn { get; init; }
    public string? LastNameEn { get; init; }
    public string? FullNameAr { get; init; }
    public string? FullNameEn { get; init; }
    public bool IsActive { get; init; }
    public string? ImageUrl { get; init; }
    public TimeOnly? TodayAttendanceTime { get; init; }
    public TimeOnly? TodayDepartureTime { get; init; }
}
