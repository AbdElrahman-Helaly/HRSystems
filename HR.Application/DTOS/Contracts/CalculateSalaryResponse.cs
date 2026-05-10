namespace internalEmployee.Auth.Contracts;

/// <summary>
/// Response model for salary calculation
/// </summary>
public sealed class CalculateSalaryResponse
{
    public string? EmployeeStatusNote { get; init; }
    public decimal GrossSalary { get; init; } // Matches the fixed amount in the user profile
    public decimal TotalEarnings { get; init; } // Sum of GrossSalary + Allowances + Overtime + Bonus
    public decimal NetSalary { get; init; }

    public AllowancesInfo Allowances { get; init; } = new();
    public DeductionsInfo Deductions { get; init; } = new();
    public InsuranceInfo Insurance { get; init; } = new();
    public decimal? InsuranceSalary { get; init; } // The base salary used for insurance calculation
    
    public decimal BonusAmount { get; init; }
    public decimal TaxAmount { get; init; }
    
    // Summary info
    public decimal? ShiftRate { get; init; }
    public int PaidShiftDays { get; init; }
    public int TotalWorkingDays { get; init; }
    public List<string> AbsenceDates { get; init; } = new();
    public List<string> LateDates { get; init; } = new();
    public decimal HoursWorked { get; init; }
    public decimal OvertimeHours { get; init; }
    public decimal OvertimePay { get; init; }
}

public sealed class AllowancesInfo
{
    public decimal Housing { get; init; }
    public decimal Meal { get; init; }
    public decimal Transportation { get; init; }
    public decimal Insurance { get; init; }
    public decimal Other { get; init; }
    public decimal Total => Housing + Meal + Transportation + Insurance + Other;
}

public sealed class DeductionsInfo
{
    public decimal LateAmount { get; init; }
    public decimal LateHours { get; init; }
    public decimal AbsenceAmount { get; init; }
    public int AbsenceDays { get; init; }
    public decimal PenaltiesAmount { get; init; }
    public decimal AdvancesAmount { get; init; }
    public decimal HealthInsuranceAmount { get; init; }
    public List<PenaltyInfo> PenaltyDetails { get; init; } = new();
    public decimal Total => LateAmount + AbsenceAmount + PenaltiesAmount + AdvancesAmount + HealthInsuranceAmount;
}

public sealed class InsuranceInfo
{
    public decimal Social { get; init; } // Employee share
    public decimal Health { get; init; } // If applicable
    public decimal CompanyShare { get; init; }
    public decimal InsuranceSalary { get; init; } // The base amount used for this calculation
    public decimal TotalDeducted => Social + Health;
}

public sealed class PenaltyInfo
{
    public int Id { get; init; }
    public string PenaltyType { get; init; } = string.Empty;
    public decimal? Days { get; init; }
    public decimal? Amount { get; init; }
    public DateOnly PenaltyDate { get; init; }
    public string? Reason { get; init; }
}
