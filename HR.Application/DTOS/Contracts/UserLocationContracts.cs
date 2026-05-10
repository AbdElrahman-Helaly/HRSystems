using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class UserLocationResponse
{
    public int Id { get; init; }
    public Guid UserId { get; init; }
    public string Name { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public int RadiusMeters { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class CreateUserLocationRequest
{
    [Required]
    public Guid UserId { get; init; }

    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Range(-90, 90)]
    public double Latitude { get; init; }

    [Range(-180, 180)]
    public double Longitude { get; init; }

    [Range(1, 1000)]
    public int RadiusMeters { get; init; } = 100;

    public bool IsActive { get; init; } = true;
}

public sealed class UpdateUserLocationRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Range(-90, 90)]
    public double Latitude { get; init; }

    [Range(-180, 180)]
    public double Longitude { get; init; }

    [Range(1, 1000)]
    public int RadiusMeters { get; init; } = 100;

    public bool IsActive { get; init; } = true;
}

