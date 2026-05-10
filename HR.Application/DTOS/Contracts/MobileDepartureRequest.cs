using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class MobileDepartureRequest
{
    [Required]
    public string FingerprintKey { get; init; } = string.Empty; // Fingerprint template from mobile

    public DateOnly? Date { get; init; } // Optional, defaults to today
    public TimeOnly? Time { get; init; } // Optional, defaults to current time

    /// <summary>
    /// Current latitude of the mobile device (in degrees).
    /// </summary>
    public double? Latitude { get; init; }

    /// <summary>
    /// Current longitude of the mobile device (in degrees).
    /// </summary>
    public double? Longitude { get; init; }

    /// <summary>
    /// Optional radius override in meters for company location check.
    /// </summary>
    public int? RadiusMeters { get; init; }
}


