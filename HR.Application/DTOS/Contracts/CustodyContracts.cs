using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class CustodyItemResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class CreateCustodyItemRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;
}

public sealed class UpdateCustodyItemRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;
}

public sealed class EmployeeCustodyResponse
{
    public int Id { get; init; }
    public Guid UserId { get; init; }
    public string? EmployeeCode { get; init; }
    public string? UserFullNameAr { get; init; }
    public string? UserFullNameEn { get; init; }
    public string? DepartmentName { get; init; }
    public string? JobTitle { get; init; }
    public int CustodyItemId { get; init; }
    public string CustodyItemName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class CreateEmployeeCustodyRequest
{
    [Required]
    public Guid UserId { get; init; }

    [Required]
    public int CustodyItemId { get; init; }

    [MaxLength(1000)]
    public string? Description { get; init; }
}

public sealed class UpdateEmployeeCustodyRequest
{
    [Required]
    public Guid UserId { get; init; }

    [Required]
    public int CustodyItemId { get; init; }

    [MaxLength(1000)]
    public string? Description { get; init; }
}

public sealed class EmployeeCustodyLookupsResponse
{
    public List<EmployeeCustodyUserLookupItem> Users { get; init; } = new();
    public List<EmployeeCustodyItemLookupItem> CustodyItems { get; init; } = new();
}

public sealed class EmployeeCustodyUserLookupItem
{
    public Guid UserId { get; init; }
    public string? EmployeeCode { get; init; }
    public string? FullNameAr { get; init; }
    public string? FullNameEn { get; init; }
    public string? DepartmentName { get; init; }
    public string? JobTitle { get; init; }
}

public sealed class EmployeeCustodyItemLookupItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
