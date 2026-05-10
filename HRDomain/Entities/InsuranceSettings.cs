namespace internalEmployee.Data.Entities;

public sealed class InsuranceSettings
{
    public int Id { get; set; }
    public decimal EmployeePercentage { get; set; }
    public decimal CompanyPercentage { get; set; }
    public decimal MinimumAmount { get; set; }
    public decimal MaximumAmount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
