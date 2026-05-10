namespace internalEmployee.Auth.Contracts;

/// <summary>
/// Response model for monthly report for an employee
/// </summary>
public sealed class MonthlyReportResponse
{
    public string? EmployeeStatusNote { get; init; }
    /// <summary>
    /// Employee user ID
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Employee name (Arabic or English)
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Department name
    /// </summary>
    public string? DepartmentName { get; init; }

    /// <summary>
    /// Job title
    /// </summary>
    public string? JobTitle { get; init; }

    /// <summary>
    /// Total number of attendance days in the month
    /// </summary>
    public int TotalAttendance { get; init; }

    /// <summary>
    /// Total number of absence days in the month
    /// </summary>
    public int TotalAbsence { get; init; }

    /// <summary>
    /// Annual leave days used in the month
    /// </summary>
    public decimal AnnualLeave { get; init; }

    /// <summary>
    /// Casual leave days used in the month
    /// </summary>
    public decimal CasualLeave { get; init; }

    /// <summary>
    /// Sick leave days used in the month
    /// </summary>
    public decimal SickLeave { get; init; }

    /// <summary>
    /// Total hours worked in the month
    /// </summary>
    public decimal TotalWorkedHoursInMonth { get; init; }

    /// <summary>
    /// Total required hours based on work schedule
    /// </summary>
    public decimal TotalHours { get; init; }

    /// <summary>
    /// Number of hours deducted (late deductions)
    /// </summary>
    public decimal HoursDeducted { get; init; }

    /// <summary>
    /// Gross salary (BaseSalary + OvertimePay)
    /// </summary>
    public decimal GrossSalary { get; init; }

    /// <summary>
    /// Employee insurance amount (deducted from salary)
    /// </summary>
    public decimal EmployeeInsuranceAmount { get; init; }

    /// <summary>
    /// Company insurance amount (for record keeping, not deducted from salary)
    /// </summary>
    public decimal CompanyInsuranceAmount { get; init; }

    /// <summary>
    /// Base salary used for insurance calculation
    /// </summary>
    public decimal InsuranceSalary { get; init; }

    /// <summary>
    /// Tax amount
    /// </summary>
    public decimal TaxAmount { get; init; }

    /// <summary>
    /// Penalty deduction amount
    /// </summary>
    public decimal PenaltyDeductionAmount { get; init; }

    /// <summary>
    /// Net salary (GrossSalary - Deductions - EmployeeInsuranceAmount - TaxAmount - PenaltyDeductionAmount)
    /// </summary>
    public decimal NetSalary { get; init; }

    /// <summary>
    /// Salary after deduction (same as NetSalary)
    /// </summary>
    public decimal SalaryAfterDeduction { get; init; }

    /// <summary>
    /// Total overtime hours in the month
    /// </summary>
    public decimal TotalOvertime { get; init; }

    /// <summary>
    /// Overtime salary amount
    /// </summary>
    public decimal OvertimeSalary { get; init; }

    /// <summary>
    /// Total bonus amount in the month
    /// </summary>
    public decimal BonusAmount { get; init; }
}
