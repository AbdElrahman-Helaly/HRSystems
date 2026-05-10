using internalEmployee.Data.Entities;

namespace internalEmployee.Auth.Contracts;

public sealed class AttendanceResponse
{
    public int Id { get; init; }
    public Guid UserId { get; init; }
    public DateOnly Date { get; init; }
    public TimeOnly? AttendanceTime { get; init; }
    public TimeOnly? DepartureTime { get; init; }
    public AttendanceDeviceType? DeviceType { get; init; } // FingerprintDevice or Mobile
    public string? Location { get; init; } // GPS location
    public int? LocationId { get; init; } // Allowed location that matched this attendance (if any)
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

