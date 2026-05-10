namespace internalEmployee.Auth.Contracts;

public sealed class HomeResponse
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
}

