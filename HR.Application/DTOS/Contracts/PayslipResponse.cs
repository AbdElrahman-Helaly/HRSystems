namespace internalEmployee.Auth.Contracts;

public sealed class PayslipResponse
{
    public string? EmployeeStatusNote { get; init; }
    // Employee Information
    public string? FullNameAr { get; init; }
    public string? FullNameEn { get; init; }
    public string? DepartmentName { get; init; }
    public string? JobTitle { get; init; }
    public string? EmploymentMode { get; init; }

    // Payment Period
    public int Month { get; init; }
    public int Year { get; init; }
    public int ActualWorkingDays { get; init; } // Total working days minus absences

    // Salary Details
    public CalculateSalaryResponse SalaryDetails { get; init; } = new();

    // Legal / Meta information
    public DateTime IssuedAt { get; init; }
}
