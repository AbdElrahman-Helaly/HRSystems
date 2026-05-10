using System;
using System.Collections.Generic;

namespace internalEmployee.Data.Entities;

public sealed class MeetingDepartment
{
    public int MeetingId { get; set; }
    public int DepartmentId { get; set; }

    // Navigation properties
    public Meeting? Meeting { get; set; }
    public Department? Department { get; set; }
}
