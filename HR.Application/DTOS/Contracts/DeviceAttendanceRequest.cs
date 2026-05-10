using System.ComponentModel.DataAnnotations;

namespace internalEmployee.Auth.Contracts;

public sealed class DeviceAttendanceRequest
{
    [Required]
    public string MachineCode { get; init; } = string.Empty; // Employee code from ZKTeco device

    public DateOnly? Date { get; init; } // Optional, defaults to today
    public TimeOnly? Time { get; init; } // Optional, defaults to current time
}


