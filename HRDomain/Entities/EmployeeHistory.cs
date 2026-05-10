namespace internalEmployee.Data.Entities;

public class EmployeeHistory
{
    public int Id { get; set; }
    
    // Employee Reference
    public Guid EmployeeId { get; set; }
    
    // Event Information
    public EmployeeEventType EventType { get; set; }
    
    // Change Details (stored as JSON for flexibility)
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    
    // Metadata
    public DateTime EventDate { get; set; } = DateTime.Now;
    public Guid? DoneBy { get; set; } // User who made the change
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    
    // Additional structured fields for common scenarios
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
    
    // For approvals
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
}
