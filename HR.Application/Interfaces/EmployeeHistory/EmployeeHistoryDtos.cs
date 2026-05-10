using internalEmployee.Data.Entities;

namespace internalEmployee.Services.EmployeeHistory;

public sealed class CreateEmployeeHistoryRequest
{
    public required Guid EmployeeId { get; set; }
    public required EmployeeEventType EventType { get; set; }
    
    // Generic values for manual entry
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    
    // Structured data (to be processed into generic values)
    public decimal? OldSalary { get; set; }
    public decimal? NewSalary { get; set; }
    public int? OldDepartmentId { get; set; }
    public int? NewDepartmentId { get; set; }
    public int? OldBranchId { get; set; }
    public int? NewBranchId { get; set; }
    public int? OldJobId { get; set; }
    public int? NewJobId { get; set; }
    public Guid? OldManagerId { get; set; }
    public Guid? NewManagerId { get; set; }
    
    public DateOnly? EffectiveDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public Guid? ApprovedBy { get; set; }
}

public sealed class EmployeeHistoryResponse
{
    public int Id { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ChangeItem> Changes { get; set; } = new(); // قائمة بكل التغييرات
    public DateTime Date { get; set; }
    public string? DoneBy { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}

public sealed class ChangeItem
{
    public string Property { get; set; } = string.Empty; // اسم الحقل (الراتب، القسم، إلخ)
    public string? From { get; set; }
    public string? To { get; set; }
}


public sealed class EmployeeHistoryListResponse
{
    public List<EmployeeHistoryResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public sealed class EmployeeSpecificHistoryResponse
{
    public string? EmployeeName { get; set; }
    public List<EmployeeHistoryItemDto> History { get; set; } = new();
    public int TotalCount { get; set; }
}

public sealed class EmployeeHistoryItemDto
{
    public int Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ChangeItem> Changes { get; set; } = new();
    public DateTime Date { get; set; }
    public string? DoneBy { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}

public sealed class EmployeeHistorySummaryResponse
{
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public int TotalEvents { get; set; }
    public int JobChanges { get; set; }
    public int SalaryChanges { get; set; }
    public int Transfers { get; set; }
    public int Promotions { get; set; }
    public int ContractEvents { get; set; }
    public int DisciplinaryActions { get; set; }
    public DateTime? LastEventDate { get; set; }
}

public sealed class GetEmployeeHistoryRequest
{
    public Guid? EmployeeId { get; set; }
    public EmployeeEventType? EventType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
