namespace internalEmployee.Data.Entities;

public enum EmployeeEventType
{
    // Job History Events
    JobTitleChange = 1,
    DepartmentChange = 2,
    BranchChange = 3,
    ManagerChange = 4,
    
    // Salary Events
    SalaryChange = 10,
    AllowanceAdded = 11,
    AllowanceRemoved = 12,
    AllowanceChange = 15,
    BonusAdded = 13,
    DeductionAdded = 14,
    
    // Transfer Events
    Transfer = 20,
    
    // Promotion Events
    Promotion = 30,
    
    // Contract Events
    ContractCreated = 40,
    ContractRenewed = 41,
    ContractTerminated = 42,
    ContractExpired = 43,
    
    // Employment Status
    Hired = 50,
    Resigned = 51,
    Terminated = 52,
    
    // Disciplinary Actions
    Warning = 60,
    Investigation = 61,
    Penalty = 62,
    Suspension = 63,

    // Leave Events
    LeaveRequest = 70
}
