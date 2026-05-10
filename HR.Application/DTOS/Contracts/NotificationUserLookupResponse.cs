namespace internalEmployee.Auth.Contracts;

public sealed class NotificationUserLookupResponse
{
    public Guid UserId { get; set; }
    public string? EmployeeCode { get; set; }
    public string? FullNameAr { get; set; }
    public string? FullNameEn { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
}
