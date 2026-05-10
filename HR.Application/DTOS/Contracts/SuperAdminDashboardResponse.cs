namespace internalEmployee.Auth.Contracts;

/// <summary>
/// Response model for SuperAdmin Dashboard
/// </summary>
public sealed class SuperAdminDashboardResponse
{
    /// <summary>
    /// Greeting message based on time of day
    /// </summary>
    public required string Greeting { get; init; }

    /// <summary>
    /// SuperAdmin full name (Arabic)
    /// </summary>
    public string? FullNameAr { get; init; }

    /// <summary>
    /// SuperAdmin full name (English)
    /// </summary>
    public string? FullNameEn { get; init; }

    /// <summary>
    /// SuperAdmin job title
    /// </summary>
    public string? JobTitle { get; init; }

    /// <summary>
    /// SuperAdmin image URL
    /// </summary>
    public string? ImageUrl { get; init; }

    /// <summary>
    /// SuperAdmin's attendance time today
    /// </summary>
    public TimeOnly? TodayAttendanceTime { get; init; }

    /// <summary>
    /// SuperAdmin's departure time today
    /// </summary>
    public TimeOnly? TodayDepartureTime { get; init; }

    /// <summary>
    /// Dashboard data for each department
    /// </summary>
    public required List<DepartmentDashboardItem> Departments { get; init; }
}
