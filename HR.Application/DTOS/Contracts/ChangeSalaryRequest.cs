namespace internalEmployee.Auth.Contracts;

public class ChangeSalaryRequest
{
    public decimal NewSalary { get; set; }
    public string? Reason { get; set; }
}
