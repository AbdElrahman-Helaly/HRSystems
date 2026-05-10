using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class ImportEmployeesForm
{
    [Required]
    public IFormFile File { get; set; } = default!;
}
