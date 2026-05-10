using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Data.Entities;

public sealed class EmployeeBankInfo
{
    [Key]
    public Guid UserId { get; set; }

    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? Iban { get; set; }
    public string? SwiftBic { get; set; }
    public string? BranchCode { get; set; }

    public internalEmployee.Auth.Models.AppUser? User { get; set; }
}


