namespace internalEmployee.Data.Entities;

public sealed class UserLocation
{
    public int Id { get; set; }
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>
    /// Allowed radius in meters around this point.
    /// </summary>
    public int RadiusMeters { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

