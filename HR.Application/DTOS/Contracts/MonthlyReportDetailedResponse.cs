namespace internalEmployee.Auth.Contracts;

public sealed class MonthlyReportDetailedResponse
{
    public string? EmployeeStatusNote { get; init; }
    public Guid UserId { get; init; }
    public required string Name { get; init; }
    public string? DepartmentName { get; init; }
    public string? JobTitle { get; init; }
    public string? EmploymentMode { get; init; } // Full Time / Part Time / Shift

    public decimal GrossSalary { get; init; }
    public decimal NetSalary { get; init; }
    public decimal ShiftRate { get; init; }
    public decimal HousingAllowance { get; init; }
    public decimal MealAllowance { get; init; }
    public decimal TransportationAllowance { get; init; }
    public decimal InsuranceAllowance { get; init; }
    public decimal BonusAmount { get; init; }
    public decimal InsuranceSalary { get; init; }

    public int TotalWorkingDays { get; init; }
    public int TotalAttendance { get; init; }
    public int TotalAbsence { get; init; }
    public List<string> AbsenceDates { get; init; } = new();

    public decimal AnnualLeave { get; init; }
    public decimal CasualLeave { get; init; }
    public decimal SickLeave { get; init; }

    public decimal TotalWorkedHoursInMonth { get; init; }
    public decimal TotalHoursRequired { get; init; }
    public decimal HoursDeducted { get; init; }
    public decimal LateDeductionAmount { get; init; }
    public decimal AbsenceDeductionAmount { get; init; }
    public decimal AdvanceDeductionAmount { get; init; }
    public decimal HealthInsuranceDeductionAmount { get; init; }
    public List<string> LateDates { get; init; } = new();

    public decimal SalaryAfterDeduction { get; init; }
    public decimal OvertimeSalary { get; init; }
}
