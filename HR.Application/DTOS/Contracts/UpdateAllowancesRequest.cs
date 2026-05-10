namespace internalEmployee.Auth.Contracts;

public sealed class UpdateAllowancesRequest
{
    public decimal HousingAllowance { get; set; }
    public decimal MealAllowance { get; set; }
    public decimal TransportationAllowance { get; set; }
    public decimal InsuranceAllowance { get; set; }
}
