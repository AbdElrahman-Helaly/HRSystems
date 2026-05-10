namespace internalEmployee.Data.Entities;

public sealed class Attendance
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? AttendanceTime { get; set; } // Time when user arrived
    public TimeOnly? DepartureTime { get; set; } // Time when user left

    // Device information
    public AttendanceDeviceType? DeviceType { get; set; } // FingerprintDevice or Mobile
    public string? Location { get; set; } // GPS location (for future use)
    public string? MachineCode { get; set; } // Machine code from ZKTeco device
    public int? LocationId { get; set; } // Allowed location that matched this attendance (if any)

    // Stored calculations (because user requested "both": store + calculate)
    public decimal? LateDeductionHours { get; set; } // e.g. 2,4,8
    public string? LateDeductionType { get; set; } // e.g. None, QuarterDay, HalfDay, FullDay
    public decimal? OvertimeHours { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}

