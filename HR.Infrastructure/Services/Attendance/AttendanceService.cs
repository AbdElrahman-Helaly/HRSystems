using internalEmployee.Auth;
using internalEmployee.Auth.Contracts;
using internalEmployee.Auth.Models;
using internalEmployee.Data;
using internalEmployee.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using AttendanceEntity = internalEmployee.Data.Entities.Attendance;

namespace internalEmployee.Services.Attendance;

public sealed class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _db;
    private readonly CompanyLocationOptions _companyLocation;
    private static readonly TimeOnly DefaultFullTimeStart = new(9, 0);
    private static readonly TimeOnly DefaultFullTimeEnd = new(17, 0);
    private static readonly HashSet<DayOfWeek> DefaultWeekDays = new()
    {
        DayOfWeek.Sunday,
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday
    };

    // Earth radius in meters for distance calculation
    private const double EarthRadiusMeters = 6371000d;

    public AttendanceService(AppDbContext db, IOptions<CompanyLocationOptions> companyLocationOptions)
    {
        _db = db;
        _companyLocation = companyLocationOptions.Value;
    }

    private static DateOnly GetEffectivePeriodEnd(DateOnly requestedEndDate)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        return requestedEndDate < today ? requestedEndDate : today;
    }

    private static (DateOnly Date, TimeOnly Time) ResolveMobileSubmittedDateTime(
        DateOnly? requestDate,
        TimeOnly? requestTime,
        DateTime serverNow)
    {
        return (
            requestDate ?? DateOnly.FromDateTime(serverNow),
            requestTime ?? TimeOnly.FromDateTime(serverNow));
    }

    private static void ValidateMobileSubmittedTime(DateOnly date, TimeOnly time, DateTime serverNow, string actionLabel)
    {
        var currentDate = DateOnly.FromDateTime(serverNow);
        var currentTime = new TimeOnly(serverNow.Hour, serverNow.Minute);
        var submittedTime = new TimeOnly(time.Hour, time.Minute);

        if (date != currentDate || submittedTime != currentTime)
        {
            throw new InvalidOperationException(
                $"وقت {actionLabel} المرسل من الموبايل يجب أن يطابق وقت السيرفر الحالي حتى الدقيقة.");
        }
    }

    public async Task<AttendanceEntity> CreateAttendanceAsync(Guid userId, CreateAttendanceRequest request, CancellationToken ct)
    {
        // Check if attendance record exists for this user and date
        var existing = await _db.Attendances
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Date == request.Date, ct);

        var attendanceTime = request.AttendanceTime ?? TimeOnly.FromDateTime(DateTime.Now);

        if (existing != null)
        {
            // Update existing record with attendance time if not already set
            if (!existing.AttendanceTime.HasValue)
            {
                existing.AttendanceTime = attendanceTime;
                existing.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
            }
            return existing;
        }
        else
        {
            // Create new record with attendance time
            var attendance = new AttendanceEntity
            {
                UserId = userId,
                Date = request.Date,
                AttendanceTime = attendanceTime
            };

            _db.Attendances.Add(attendance);
            await _db.SaveChangesAsync(ct);
            return attendance;
        }
    }

    public async Task<AttendanceEntity> CreateDepartureAsync(Guid userId, CreateDepartureRequest request, CancellationToken ct)
    {
        // Check if attendance record exists for this user and date
        var existing = await _db.Attendances
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Date == request.Date, ct);

        var departureTime = request.DepartureTime ?? TimeOnly.FromDateTime(DateTime.Now);

        if (existing != null)
        {
            // Update existing record with departure time
            existing.DepartureTime = departureTime;
            existing.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);
            return existing;
        }
        else
        {
            // Create new record with only departure time (unusual but allowed)
            var attendance = new AttendanceEntity
            {
                UserId = userId,
                Date = request.Date,
                DepartureTime = departureTime
            };

            _db.Attendances.Add(attendance);
            await _db.SaveChangesAsync(ct);
            return attendance;
        }
    }

    // Device (ZKTeco) attendance methods
    public async Task<AttendanceEntity> CreateDeviceAttendanceAsync(DeviceAttendanceRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Find user by MachineCode
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.MachineCode == request.MachineCode, ct);

        if (user == null)
            throw new InvalidOperationException($"User with MachineCode '{request.MachineCode}' not found.");

        if (!user.IsActive)
            throw new InvalidOperationException("User is not active.");

        var date = request.Date ?? DateOnly.FromDateTime(DateTime.Now);
        var attendanceTime = request.Time ?? TimeOnly.FromDateTime(DateTime.Now);

        // Check if attendance record exists for this user and date
        var existing = await _db.Attendances
            .FirstOrDefaultAsync(a => a.UserId == user.Id && a.Date == date, ct);

        if (existing != null)
        {
            // Update existing record with attendance time if not already set
            if (!existing.AttendanceTime.HasValue)
            {
                existing.AttendanceTime = attendanceTime;
                existing.DeviceType = AttendanceDeviceType.FingerprintDevice;
                existing.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
            }
            return existing;
        }
        else
        {
            // Create new record with attendance time
            var attendance = new AttendanceEntity
            {
                UserId = user.Id,
                Date = date,
                AttendanceTime = attendanceTime,
                DeviceType = AttendanceDeviceType.FingerprintDevice
            };

            _db.Attendances.Add(attendance);
            await _db.SaveChangesAsync(ct);
            return attendance;
        }
    }

    public async Task<AttendanceEntity> CreateDeviceDepartureAsync(DeviceDepartureRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Find user by MachineCode
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.MachineCode == request.MachineCode, ct);

        if (user == null)
            throw new InvalidOperationException($"User with MachineCode '{request.MachineCode}' not found.");

        if (!user.IsActive)
            throw new InvalidOperationException("User is not active.");

        var date = request.Date ?? DateOnly.FromDateTime(DateTime.Now);
        var departureTime = request.Time ?? TimeOnly.FromDateTime(DateTime.Now);

        // Check if attendance record exists for this user and date
        var existing = await _db.Attendances
            .FirstOrDefaultAsync(a => a.UserId == user.Id && a.Date == date, ct);

        if (existing != null)
        {
            // Update existing record with departure time
            existing.DepartureTime = departureTime;
            if (existing.DeviceType == null)
                existing.DeviceType = AttendanceDeviceType.FingerprintDevice;
            existing.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);
            return existing;
        }
        else
        {
            // Create new record with only departure time
            var attendance = new AttendanceEntity
            {
                UserId = user.Id,
                Date = date,
                DepartureTime = departureTime,
                DeviceType = AttendanceDeviceType.FingerprintDevice
            };

            _db.Attendances.Add(attendance);
            await _db.SaveChangesAsync(ct);
            return attendance;
        }
    }

    public async Task<DeviceSyncResponse> SyncDeviceAttendanceAsync(DeviceSyncRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Find user by MachineCode
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.MachineCode == request.MachineCode, ct);

        if (user == null)
        {
            return new DeviceSyncResponse
            {
                IsNew = false,
                IsUpdated = false,
                IsSkipped = false,
                Message = $"User with MachineCode '{request.MachineCode}' not found."
            };
        }

        if (!user.IsActive)
        {
            return new DeviceSyncResponse
            {
                IsNew = false,
                IsUpdated = false,
                IsSkipped = false,
                Message = "User is not active."
            };
        }

        // Check if attendance record exists for this user and date
        var existing = await _db.Attendances
            .FirstOrDefaultAsync(a => a.UserId == user.Id && a.Date == request.Date, ct);

        if (existing != null)
        {
            // Check if data is the same
            bool checkInMatches = existing.AttendanceTime == request.CheckInTime;
            bool checkOutMatches = existing.DepartureTime == request.CheckOutTime;

            if (checkInMatches && checkOutMatches)
            {
                // Same data - skip
                return new DeviceSyncResponse
                {
                    IsNew = false,
                    IsUpdated = false,
                    IsSkipped = true,
                    Message = "Record already exists with the same data."
                };
            }

            // Data is different - update only if new data is better (fills missing values)
            bool updated = false;

            if (request.CheckInTime.HasValue && !existing.AttendanceTime.HasValue)
            {
                existing.AttendanceTime = request.CheckInTime.Value;
                updated = true;
            }

            if (request.CheckOutTime.HasValue && !existing.DepartureTime.HasValue)
            {
                existing.DepartureTime = request.CheckOutTime.Value;
                updated = true;
            }

            if (updated)
            {
                if (existing.DeviceType == null)
                    existing.DeviceType = AttendanceDeviceType.FingerprintDevice;
                if (string.IsNullOrEmpty(existing.MachineCode))
                    existing.MachineCode = request.MachineCode;
                existing.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);

                return new DeviceSyncResponse
                {
                    IsNew = false,
                    IsUpdated = true,
                    IsSkipped = false,
                    Message = "Record updated with new data."
                };
            }
            else
            {
                // Data exists but no update needed (existing data is better or same)
                return new DeviceSyncResponse
                {
                    IsNew = false,
                    IsUpdated = false,
                    IsSkipped = true,
                    Message = "Record exists and no update needed."
                };
            }
        }
        else
        {
            // Create new record
            var attendance = new AttendanceEntity
            {
                UserId = user.Id,
                Date = request.Date,
                AttendanceTime = request.CheckInTime,
                DepartureTime = request.CheckOutTime,
                DeviceType = AttendanceDeviceType.FingerprintDevice,
                MachineCode = request.MachineCode
            };

            _db.Attendances.Add(attendance);
            await _db.SaveChangesAsync(ct);

            return new DeviceSyncResponse
            {
                IsNew = true,
                IsUpdated = false,
                IsSkipped = false,
                Message = "New record created successfully."
            };
        }
    }

    public async Task<DeviceBatchSyncResponse> SyncDeviceAttendanceBatchAsync(DeviceBatchSyncRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (request.Records == null || request.Records.Count == 0)
        {
            return new DeviceBatchSyncResponse
            {
                SavedCount = 0,
                SkippedCount = 0,
                NotFoundCount = 0,
                NotFoundMachineCodes = new List<string>(),
                Results = new List<DeviceSyncItemResponse>()
            };
        }

        // Get all users with MachineCode for quick lookup
        var machineCodes = request.Records.Select(r => r.MachineCode).Distinct().ToList();
        var users = await _db.Users
            .Where(u => u.MachineCode != null && machineCodes.Contains(u.MachineCode))
            .ToDictionaryAsync(u => u.MachineCode!, u => u, ct);

        var results = new List<DeviceSyncItemResponse>();
        var notFoundMachineCodes = new HashSet<string>();
        int savedCount = 0;
        int skippedCount = 0;
        int notFoundCount = 0;

        // Process each record
        foreach (var record in request.Records)
        {
            // Find user by MachineCode
            if (!users.TryGetValue(record.MachineCode, out var user))
            {
                notFoundMachineCodes.Add(record.MachineCode);
                notFoundCount++;
                results.Add(new DeviceSyncItemResponse
                {
                    MachineCode = record.MachineCode,
                    Date = record.Date,
                    IsNew = false,
                    IsUpdated = false,
                    IsSkipped = false,
                    IsNotFound = true,
                    Message = $"User with MachineCode '{record.MachineCode}' not found."
                });
                continue;
            }

            if (!user.IsActive)
            {
                notFoundMachineCodes.Add(record.MachineCode);
                notFoundCount++;
                results.Add(new DeviceSyncItemResponse
                {
                    MachineCode = record.MachineCode,
                    Date = record.Date,
                    IsNew = false,
                    IsUpdated = false,
                    IsSkipped = false,
                    IsNotFound = true,
                    Message = $"User with MachineCode '{record.MachineCode}' is not active."
                });
                continue;
            }

            // Check if attendance record exists for this user and date
            var existing = await _db.Attendances
                .FirstOrDefaultAsync(a => a.UserId == user.Id && a.Date == record.Date, ct);

            if (existing != null)
            {
                // Check if data is the same
                bool checkInMatches = existing.AttendanceTime == record.CheckInTime;
                bool checkOutMatches = existing.DepartureTime == record.CheckOutTime;

                if (checkInMatches && checkOutMatches)
                {
                    // Same data - skip
                    skippedCount++;
                    results.Add(new DeviceSyncItemResponse
                    {
                        MachineCode = record.MachineCode,
                        Date = record.Date,
                        IsNew = false,
                        IsUpdated = false,
                        IsSkipped = true,
                        IsNotFound = false,
                        Message = "Record already exists with the same data."
                    });
                    continue;
                }

                // Data is different - update only if new data is better (fills missing values)
                bool updated = false;

                if (record.CheckInTime.HasValue && !existing.AttendanceTime.HasValue)
                {
                    existing.AttendanceTime = record.CheckInTime.Value;
                    updated = true;
                }

                if (record.CheckOutTime.HasValue && !existing.DepartureTime.HasValue)
                {
                    existing.DepartureTime = record.CheckOutTime.Value;
                    updated = true;
                }

                if (updated)
                {
                    if (existing.DeviceType == null)
                        existing.DeviceType = AttendanceDeviceType.FingerprintDevice;
                    if (string.IsNullOrEmpty(existing.MachineCode))
                        existing.MachineCode = record.MachineCode;
                    existing.UpdatedAt = DateTime.Now;
                    savedCount++;
                    results.Add(new DeviceSyncItemResponse
                    {
                        MachineCode = record.MachineCode,
                        Date = record.Date,
                        IsNew = false,
                        IsUpdated = true,
                        IsSkipped = false,
                        IsNotFound = false,
                        Message = "Record updated with new data."
                    });
                }
                else
                {
                    // Data exists but no update needed
                    skippedCount++;
                    results.Add(new DeviceSyncItemResponse
                    {
                        MachineCode = record.MachineCode,
                        Date = record.Date,
                        IsNew = false,
                        IsUpdated = false,
                        IsSkipped = true,
                        IsNotFound = false,
                        Message = "Record exists and no update needed."
                    });
                }
            }
            else
            {
                // Create new record
                var attendance = new AttendanceEntity
                {
                    UserId = user.Id,
                    Date = record.Date,
                    AttendanceTime = record.CheckInTime,
                    DepartureTime = record.CheckOutTime,
                    DeviceType = AttendanceDeviceType.FingerprintDevice,
                    MachineCode = record.MachineCode
                };

                _db.Attendances.Add(attendance);
                savedCount++;
                results.Add(new DeviceSyncItemResponse
                {
                    MachineCode = record.MachineCode,
                    Date = record.Date,
                    IsNew = true,
                    IsUpdated = false,
                    IsSkipped = false,
                    IsNotFound = false,
                    Message = "New record created successfully."
                });
            }
        }

        // Save all changes at once
        await _db.SaveChangesAsync(ct);

        return new DeviceBatchSyncResponse
        {
            SavedCount = savedCount,
            SkippedCount = skippedCount,
            NotFoundCount = notFoundCount,
            NotFoundMachineCodes = notFoundMachineCodes.ToList(),
            Results = results
        };
    }

    // Mobile attendance methods
    public async Task<AttendanceEntity> CreateMobileAttendanceAsync(Guid userId, MobileAttendanceRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Find user by userId from token
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
                throw new InvalidOperationException("User not found.");

        if (!user.IsActive)
            throw new InvalidOperationException("User is not active.");

        // Validate location against allowed locations
        var (latitude, longitude) = ValidateMobileLocation(request.Latitude, request.Longitude);
        var matchedLocationId = await ResolveLocationIdOrThrowAsync(
            user.Id,
            latitude,
            longitude,
            request.RadiusMeters,
            user.AllowMobileAttendanceFromAnyLocation,
            ct);
        var mobileAttendanceLocation = FormatMobileLocationEntry("Attendance", latitude, longitude);

        // Handle fingerprint registration/verification
        if (string.IsNullOrWhiteSpace(user.FingerprintKey))
        {
            var fingerprintKey = request.FingerprintKey?.Trim();
            if (string.IsNullOrWhiteSpace(fingerprintKey))
                throw new InvalidOperationException("Fingerprint is required to register.");

            var fingerprintExists = await _db.Users
                .AnyAsync(u => u.FingerprintKey == fingerprintKey && u.Id != user.Id, ct);
            if (fingerprintExists)
                throw new InvalidOperationException("Fingerprint already registered to another employee.");

            // First time registration: Save the fingerprint
            user.FingerprintKey = fingerprintKey;
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            // Verify fingerprint matches the saved one
            if (user.FingerprintKey != request.FingerprintKey)
                throw new InvalidOperationException("Invalid fingerprint.");
        }

        var serverNow = DateTime.Now;
        var (date, attendanceTime) = ResolveMobileSubmittedDateTime(request.Date, request.Time, serverNow);
        ValidateMobileSubmittedTime(date, attendanceTime, serverNow, "الحضور");

        // Check if attendance record exists for this user and date
        var existing = await _db.Attendances
            .FirstOrDefaultAsync(a => a.UserId == user.Id && a.Date == date, ct);

        if (existing != null)
        {
            // Update existing record with attendance time if not already set
            if (existing.AttendanceTime.HasValue)
                throw new InvalidOperationException("تم تسجيل الحضور مسبقا.");

            existing.AttendanceTime = attendanceTime;
            existing.DeviceType = AttendanceDeviceType.Mobile;
            existing.Location = MergeMobileLocationEntry(existing.Location, mobileAttendanceLocation);
            existing.LocationId = matchedLocationId;
            existing.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);
            return existing;
        }
        else
        {
            // Create new record with attendance time
            var attendance = new AttendanceEntity
            {
                UserId = user.Id,
                Date = date,
                AttendanceTime = attendanceTime,
                DeviceType = AttendanceDeviceType.Mobile,
                Location = mobileAttendanceLocation,
                LocationId = matchedLocationId
            };

            _db.Attendances.Add(attendance);
            await _db.SaveChangesAsync(ct);
            return attendance;
        }
    }

    public async Task<AttendanceEntity> CreateMobileDepartureAsync(Guid userId, MobileDepartureRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Find user by userId from token
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
            throw new InvalidOperationException("User not found.");

        if (!user.IsActive)
            throw new InvalidOperationException("User is not active.");

        // Validate location against allowed locations
        var (latitude, longitude) = ValidateMobileLocation(request.Latitude, request.Longitude);
        var matchedLocationId = await ResolveLocationIdOrThrowAsync(
            user.Id,
            latitude,
            longitude,
            request.RadiusMeters,
            user.AllowMobileAttendanceFromAnyLocation,
            ct);
        var mobileDepartureLocation = FormatMobileLocationEntry("Departure", latitude, longitude);

        // Handle fingerprint registration/verification (allow first-time registration on checkout)
        if (string.IsNullOrWhiteSpace(user.FingerprintKey))
        {
            var fingerprintKey = request.FingerprintKey?.Trim();
            if (string.IsNullOrWhiteSpace(fingerprintKey))
                throw new InvalidOperationException("Fingerprint is required to register.");

            var fingerprintExists = await _db.Users
                .AnyAsync(u => u.FingerprintKey == fingerprintKey && u.Id != user.Id, ct);
            if (fingerprintExists)
                throw new InvalidOperationException("Fingerprint already registered to another employee.");

            user.FingerprintKey = fingerprintKey;
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            if (user.FingerprintKey != request.FingerprintKey)
                throw new InvalidOperationException("Invalid fingerprint.");
        }

        var serverNow = DateTime.Now;
        var (date, departureTime) = ResolveMobileSubmittedDateTime(request.Date, request.Time, serverNow);
        ValidateMobileSubmittedTime(date, departureTime, serverNow, "الانصراف");

        // Check if attendance record exists for this user and date
        var existing = await _db.Attendances
            .FirstOrDefaultAsync(a => a.UserId == user.Id && a.Date == date, ct);

        if (existing != null)
        {
            // Must have attendance before departure
            if (!existing.AttendanceTime.HasValue)
                throw new InvalidOperationException("لا يوجد تسجيل حضور اليوم.");

            // Update existing record with departure time
            if (existing.DepartureTime.HasValue)
                throw new InvalidOperationException("تم تسجيل الانصراف مسبقا.");

            existing.DepartureTime = departureTime;
            if (existing.DeviceType == null)
                existing.DeviceType = AttendanceDeviceType.Mobile;
            existing.Location = MergeMobileLocationEntry(existing.Location, mobileDepartureLocation);
            if (matchedLocationId.HasValue)
                existing.LocationId = matchedLocationId;
            existing.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);
            return existing;
        }
        else
        {
            throw new InvalidOperationException("لا يوجد تسجيل حضور اليوم.");
        }
    }

    public async Task<AttendanceEntity> RecordManualAttendanceAsync(ManualAttendanceRequest request, CancellationToken ct)
    {
        // Check if user exists
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

        if (user == null)
            throw new InvalidOperationException("User not found.");

        // Check if attendance record exists for this user and date
        var existing = await _db.Attendances
            .FirstOrDefaultAsync(a => a.UserId == request.UserId && a.Date == request.Date, ct);

        if (existing != null)
        {
            // Update existing record
            if (request.AttendanceTime.HasValue)
                existing.AttendanceTime = request.AttendanceTime;
            
            if (request.DepartureTime.HasValue)
                existing.DepartureTime = request.DepartureTime;

            // Only update DeviceType if it's not set or force it? 
            // User asked to "register manually", implying this IS a manual entry.
            // If we overwrite, we lose info that it was originally from device.
            // But if HR edits it, it becomes "Manual" override.
            existing.DeviceType = AttendanceDeviceType.Manual;
            existing.UpdatedAt = DateTime.Now;
            
            await _db.SaveChangesAsync(ct);
            return existing;
        }
        else
        {
            // Create new record
            var attendance = new AttendanceEntity
            {
                UserId = request.UserId,
                Date = request.Date,
                AttendanceTime = request.AttendanceTime,
                DepartureTime = request.DepartureTime,
                DeviceType = AttendanceDeviceType.Manual,
                CreatedAt = DateTime.Now
            };

            _db.Attendances.Add(attendance);
            await _db.SaveChangesAsync(ct);
            return attendance;
        }
    }

    public async Task<List<AttendanceEntity>> GetUserAttendancesAsync(Guid userId, DateOnly? startDate, DateOnly? endDate, CancellationToken ct)
    {
        var query = _db.Attendances
            .Where(a => a.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(a => a.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.Date <= endDate.Value);

        return await query
            .OrderByDescending(a => a.Date)
            .ToListAsync(ct);
    }

    public async Task<AttendanceEntity?> GetAttendanceByDateAsync(Guid userId, DateOnly date, CancellationToken ct)
    {
        return await _db.Attendances
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Date == date, ct);
    }

    public async Task<List<AttendanceEntity>> GetAllAttendancesAsync(Guid userId, int? month, CancellationToken ct)
    {
        var query = _db.Attendances
            .Where(a => a.UserId == userId);

        if (month.HasValue && month.Value >= 1 && month.Value <= 12)
        {
            // Filter by month of the Date field (DateOnly)
            // EF Core can handle DateOnly.Month in queries
            query = query.Where(a => a.Date.Month == month.Value);
        }

        return await query
            .OrderByDescending(a => a.Date)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<AttendanceEntity>> GetAllAttendancesWithDateFilterAsync(Guid userId, DateOnly? startDate, DateOnly? endDate, CancellationToken ct)
    {
        var query = _db.Attendances
            .Where(a => a.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(a => a.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.Date <= endDate.Value);

        return await query
            .OrderByDescending(a => a.Date)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<AttendanceEntity>> GetUserAttendanceRecordsAsync(Guid userId, int? month, CancellationToken ct)
    {
        // Get only records where AttendanceTime is not null
        var query = _db.Attendances
            .Where(a => a.UserId == userId && a.AttendanceTime.HasValue);

        if (month.HasValue && month.Value >= 1 && month.Value <= 12)
        {
            query = query.Where(a => a.Date.Month == month.Value);
        }

        return await query
            .OrderByDescending(a => a.Date)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<AttendanceEntity>> GetUserDepartureRecordsAsync(Guid userId, int? month, CancellationToken ct)
    {
        // Get only records where DepartureTime is not null
        var query = _db.Attendances
            .Where(a => a.UserId == userId && a.DepartureTime.HasValue);

        if (month.HasValue && month.Value >= 1 && month.Value <= 12)
        {
            query = query.Where(a => a.Date.Month == month.Value);
        }

        return await query
            .OrderByDescending(a => a.Date)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<MonthlyAttendanceReportResponse> GetMonthlyReportAsync(Guid userId, int month, int year, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (month < 1 || month > 12)
            throw new InvalidOperationException("Invalid month.");
        if (year < 2000 || year > 2100)
            throw new InvalidOperationException("Invalid year.");

        // Validate that the requested month is not in the future
        var currentDate = DateTime.Now;
        var requestedDate = new DateOnly(year, month, 1);
        var currentMonthStart = new DateOnly(currentDate.Year, currentDate.Month, 1);
        
        if (requestedDate > currentMonthStart)
            throw new InvalidOperationException("لا يمكن طلب تقرير لشهر لم يأت بعد.");

        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.WorkSchedule)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
            throw new InvalidOperationException("Employee not found.");

        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var attendances = await _db.Attendances
            .Where(a => a.UserId == userId && a.Date >= startDate && a.Date <= endDate)
            .OrderBy(a => a.Date)
            .ToListAsync(ct);

        var byDate = attendances.ToDictionary(a => a.Date);
        var days = new List<MonthlyAttendanceReportDayItem>();

        decimal totalLateDeductionHours = 0m;
        decimal totalOvertimeHours = 0m;

        string? modeName = null;
        if (user.EmploymentModeId.HasValue)
        {
            modeName = await _db.EmploymentModes
                .AsNoTracking()
                .Where(m => m.Id == user.EmploymentModeId.Value)
                .Select(m => m.Name)
                .FirstOrDefaultAsync(ct);
        }
        var isShift = string.Equals(modeName, "Shift", StringComparison.OrdinalIgnoreCase);

        // Update stored fields (both)
        var anyChanged = false;

        for (var d = startDate; d <= endDate; d = d.AddDays(1))
        {
            byDate.TryGetValue(d, out var attendance);

            var (scheduledStart, scheduledEnd) = await ResolveScheduledTimesByEmploymentModeAsync(user, d, ct);

            var (lateMinutes, lateType, lateHours) = CalculateLateDeduction(attendance?.AttendanceTime, scheduledStart);
            var overtimeHours = 0m;
            if (attendance != null)
            {
                overtimeHours = isShift
                    ? CalculateShiftOvertimeHours(attendance)
                    : CalculateOvertime(attendance.DepartureTime, scheduledEnd);
            }

            totalLateDeductionHours += lateHours;
            totalOvertimeHours += overtimeHours;

            if (attendance != null)
            {
                var newLateType = lateType;
                var newLateHours = lateHours == 0m ? (decimal?)null : lateHours;
                var newOtHours = overtimeHours == 0m ? (decimal?)null : overtimeHours;

                if (attendance.LateDeductionType != newLateType
                    || attendance.LateDeductionHours != newLateHours
                    || attendance.OvertimeHours != newOtHours)
                {
                    attendance.LateDeductionType = newLateType;
                    attendance.LateDeductionHours = newLateHours;
                    attendance.OvertimeHours = newOtHours;
                    attendance.UpdatedAt = DateTime.Now;
                    anyChanged = true;
                }
            }

            days.Add(new MonthlyAttendanceReportDayItem
            {
                Date = d,
                ScheduledStartTime = scheduledStart,
                ScheduledEndTime = scheduledEnd,
                AttendanceTime = attendance?.AttendanceTime,
                DepartureTime = attendance?.DepartureTime,
                LateMinutes = lateMinutes,
                LateDeductionType = lateType,
                LateDeductionHours = lateHours,
                OvertimeHours = overtimeHours
            });
        }

        if (anyChanged)
            await _db.SaveChangesAsync(ct);

        return new MonthlyAttendanceReportResponse
        {
            UserId = userId,
            Month = month,
            Year = year,
            TotalLateDeductionHours = totalLateDeductionHours,
            TotalOvertimeHours = totalOvertimeHours,
            OvertimeRate = user.OvertimeRate,
            Days = days
        };
    }

    private async Task<(TimeOnly? Start, TimeOnly? End)> ResolveScheduledTimesByEmploymentModeAsync(
        internalEmployee.Auth.Models.AppUser user,
        DateOnly date,
        CancellationToken ct)
    {
        // If EmploymentMode not set => treat as Full Time
        string? modeName = null;
        if (user.EmploymentModeId.HasValue)
        {
            modeName = await _db.EmploymentModes
                .AsNoTracking()
                .Where(m => m.Id == user.EmploymentModeId.Value)
                .Select(m => m.Name)
                .FirstOrDefaultAsync(ct);
        }

        var isPartTime = string.Equals(modeName, "Part Time", StringComparison.OrdinalIgnoreCase);
        var isShift = string.Equals(modeName, "Shift", StringComparison.OrdinalIgnoreCase);

        if (isPartTime)
        {
            var partTimeDays = ResolvePartTimeWorkDays(user.WorkSchedule);
            if (!partTimeDays.Contains(date.DayOfWeek))
                return (null, null);

            return (user.WorkSchedule?.PartTimeStart, user.WorkSchedule?.PartTimeEnd);
        }

        if (isShift)
            return (null, null);

        // Full Time (default)
        var start = user.WorkSchedule?.FullTimeStartOverride ?? DefaultFullTimeStart;
        var end = user.WorkSchedule?.FullTimeEndOverride ?? DefaultFullTimeEnd;
        return (start, end);
    }

    private static bool IsFullTimeWeekday(DayOfWeek dayOfWeek)
        => dayOfWeek != DayOfWeek.Friday && dayOfWeek != DayOfWeek.Saturday;

    private static HashSet<DayOfWeek> ResolvePartTimeWorkDays(EmployeeWorkSchedule? workSchedule)
    {
        if (workSchedule == null || workSchedule.PartTimeUseDefaultWeek)
            return new HashSet<DayOfWeek>(DefaultWeekDays);

        if (string.IsNullOrWhiteSpace(workSchedule.PartTimeCustomDaysJson))
            return new HashSet<DayOfWeek>();

        try
        {
            var parsed = JsonSerializer.Deserialize<List<DayOfWeek>>(workSchedule.PartTimeCustomDaysJson);
            return parsed == null
                ? new HashSet<DayOfWeek>()
                : parsed.Distinct().ToHashSet();
        }
        catch
        {
            return new HashSet<DayOfWeek>();
        }
    }

    private static (int? LateMinutes, string LateType, decimal LateHours) CalculateLateDeduction(TimeOnly? attendanceTime, TimeOnly? scheduledStart)
    {
        if (!attendanceTime.HasValue || !scheduledStart.HasValue)
            return (null, "None", 0m);

        var diffMinutes = (int)(attendanceTime.Value.ToTimeSpan() - scheduledStart.Value.ToTimeSpan()).TotalMinutes;
        if (diffMinutes <= 0)
            return (0, "None", 0m);

        if (diffMinutes <= 15)
            return (diffMinutes, "None", 0m); // مسموح - لا خصم

        if (diffMinutes <= 30)
            return (diffMinutes, "QuarterDay", 2.0m); // ربع يوم (2 ساعة من 8)

        if (diffMinutes <= 59)
            return (diffMinutes, "HalfDay", 4.0m); // نصف يوم (4 ساعات من 8)

        return (diffMinutes, "FullDay", 8.0m); // يوم كامل (8 ساعات)
    }

    public async Task<int> SaveDeviceAttendanceLogsAsync(List<DeviceAttendanceResponse> logs, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        int savedCount = 0;

        // Get all users with their MachineCodes for quick lookup
        // Use GroupBy to handle duplicate MachineCodes (take first active user)
        var usersList = await _db.Users
            .Where(u => u.MachineCode != null && u.IsActive)
            .ToListAsync(ct);

        // Create dictionary, handling duplicate MachineCodes by taking the first one
        var users = usersList
            .GroupBy(u => u.MachineCode!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var log in logs)
        {
            try
            {
                // Find user by MachineCode
                if (!users.TryGetValue(log.MachineCode, out var user))
                {
                    // Skip if user not found
                    continue;
                }

                // Check if attendance record exists for this user and date
                var existing = await _db.Attendances
                    .FirstOrDefaultAsync(a => a.UserId == user.Id && a.Date == log.Date, ct);

                if (existing != null)
                {
                    // Update existing record
                    bool updated = false;

                    if (log.CheckInTime.HasValue && !existing.AttendanceTime.HasValue)
                    {
                        existing.AttendanceTime = log.CheckInTime.Value;
                        updated = true;
                    }

                    if (log.CheckOutTime.HasValue && !existing.DepartureTime.HasValue)
                    {
                        existing.DepartureTime = log.CheckOutTime.Value;
                        updated = true;
                    }

                    if (updated)
                    {
                        if (existing.DeviceType == null)
                            existing.DeviceType = AttendanceDeviceType.FingerprintDevice;
                        if (log.Location != null)
                            existing.Location = log.Location;
                        existing.UpdatedAt = DateTime.Now;
                        savedCount++;
                    }
                }
                else
                {
                    // Create new record
                    var attendance = new AttendanceEntity
                    {
                        UserId = user.Id,
                        Date = log.Date,
                        AttendanceTime = log.CheckInTime,
                        DepartureTime = log.CheckOutTime,
                        DeviceType = AttendanceDeviceType.FingerprintDevice,
                        Location = log.Location
                    };

                    _db.Attendances.Add(attendance);
                    savedCount++;
                }
            }
            catch (Exception)
            {
                // Continue with next log if error occurs
                continue;
            }
        }

        await _db.SaveChangesAsync(ct);
        return savedCount;
    }

    public async Task<List<AllAttendancesResponse>> GetAllAttendancesWithUserInfoAsync(DateOnly? startDate, DateOnly? endDate, string? machineCode, Guid? employeeId, bool? isCheckIn, int pageNumber, int pageSize, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Determine date range (default to last 30 days if not specified)
        var actualStartDate = startDate ?? DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
        var actualEndDate = endDate ?? DateOnly.FromDateTime(DateTime.Now);

        // Get all active users (or specific user by machineCode or employeeId)
        var usersQuery = _db.Users
            .Where(u => u.IsActive);

        if (!string.IsNullOrWhiteSpace(machineCode))
        {
            usersQuery = usersQuery.Where(u => u.MachineCode != null && u.MachineCode.ToLower() == machineCode.ToLower().Trim());
        }

        if (employeeId.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.Id == employeeId.Value);
        }

        var users = await usersQuery.ToListAsync(ct);

        // Get all attendances in the date range
        var attendances = await _db.Attendances
            .Where(a => a.Date >= actualStartDate && a.Date <= actualEndDate)
            .ToListAsync(ct);

        // Create a dictionary for quick lookup: (UserId, Date) -> Attendance
        var attendanceDict = attendances
            .GroupBy(a => new { a.UserId, a.Date })
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.CreatedAt).First());

        var results = new List<AllAttendancesResponse>();

        // For each user, create entries for all days in the period
        foreach (var user in users)
        {
            // Build employee name (prefer Arabic, fallback to English)
            string? employeeName = null;
            if (!string.IsNullOrWhiteSpace(user.FirstNameAr) || !string.IsNullOrWhiteSpace(user.LastNameAr))
            {
                employeeName = $"{user.FirstNameAr} {user.MiddleNameAr} {user.LastNameAr}".Trim();
            }
            else if (!string.IsNullOrWhiteSpace(user.FirstNameEn) || !string.IsNullOrWhiteSpace(user.LastNameEn))
            {
                employeeName = $"{user.FirstNameEn} {user.MiddleNameEn} {user.LastNameEn}".Trim();
            }

            // Generate entries for all days in the period
            for (var date = actualStartDate; date <= actualEndDate; date = date.AddDays(1))
            {
                var key = new { UserId = user.Id, Date = date };
                
                if (attendanceDict.TryGetValue(key, out var attendance))
                {
                    // Existing attendance record
                    var dayOfWeek = attendance.Date.DayOfWeek;
                    string dayOfWeekName = GetDayOfWeekName(dayOfWeek);

                    results.Add(new AllAttendancesResponse
                    {
                        Id = attendance.Id,
                        EmployeeName = employeeName,
                        MachineCode = user.MachineCode,
                        Date = attendance.Date,
                        DayOfWeek = dayOfWeekName,
                        AttendanceTime = attendance.AttendanceTime,
                        DepartureTime = attendance.DepartureTime,
                        DeviceType = attendance.DeviceType,
                        Location = attendance.Location,
                        LocationId = attendance.LocationId,
                        CreatedAt = attendance.CreatedAt,
                        UpdatedAt = attendance.UpdatedAt
                    });
                }
                else
                {
                    // No attendance record - create entry with null times
                    var dayOfWeek = date.DayOfWeek;
                    string dayOfWeekName = GetDayOfWeekName(dayOfWeek);

                    results.Add(new AllAttendancesResponse
                    {
                        Id = 0, // No database ID for missing records
                        EmployeeName = employeeName,
                        MachineCode = user.MachineCode,
                        Date = date,
                        DayOfWeek = dayOfWeekName,
                        AttendanceTime = null,
                        DepartureTime = null,
                        DeviceType = null,
                        Location = null,
                        LocationId = null,
                        CreatedAt = DateTime.MinValue, // Use MinValue to indicate no record
                        UpdatedAt = null
                    });
                }
            }
        }

        // Apply isCheckIn filter if specified
        if (isCheckIn.HasValue)
        {
            if (isCheckIn.Value)
            {
                // Filter: show only records with AttendanceTime != null
                results = results.Where(r => r.AttendanceTime.HasValue).ToList();
            }
            else
            {
                // Filter: show only records with DepartureTime != null
                results = results.Where(r => r.DepartureTime.HasValue).ToList();
            }
        }

        // Sort by date descending, then by attendance time descending, then by employee name
        return results
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.AttendanceTime)
            .ThenBy(x => x.EmployeeName ?? "")
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public async Task<AllAttendancesWithSummaryResponse> GetAllAttendancesWithSummaryAsync(DateOnly? startDate, DateOnly? endDate, string? machineCode, Guid? employeeId, bool? isCheckIn, int pageNumber, int pageSize, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Determine date range (default to last 30 days if not specified)
        var actualStartDate = startDate ?? DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
        var actualEndDate = endDate ?? DateOnly.FromDateTime(DateTime.Now);

        // Get all active users (or specific user by machineCode or employeeId)
        var usersQuery = _db.Users
            .Where(u => u.IsActive);

        if (!string.IsNullOrWhiteSpace(machineCode))
        {
            usersQuery = usersQuery.Where(u => u.MachineCode != null && u.MachineCode.ToLower() == machineCode.ToLower().Trim());
        }

        if (employeeId.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.Id == employeeId.Value);
        }

        var users = await usersQuery.ToListAsync(ct);
        var totalEmployees = users.Count;

        // Get all attendances in the date range
        var attendances = await _db.Attendances
            .Where(a => a.Date >= actualStartDate && a.Date <= actualEndDate)
            .ToListAsync(ct);

        // Create a dictionary for quick lookup: (UserId, Date) -> Attendance
        var attendanceDict = attendances
            .GroupBy(a => new { a.UserId, a.Date })
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.CreatedAt).First());

        var results = new List<AllAttendancesResponse>();
        var employeesWithAttendanceSet = new HashSet<Guid>();
        var employeesWithDepartureSet = new HashSet<Guid>();

        // For each user, create entries for all days in the period
        foreach (var user in users)
        {
            // Build employee name (prefer Arabic, fallback to English)
            string? employeeName = null;
            if (!string.IsNullOrWhiteSpace(user.FirstNameAr) || !string.IsNullOrWhiteSpace(user.LastNameAr))
            {
                employeeName = $"{user.FirstNameAr} {user.MiddleNameAr} {user.LastNameAr}".Trim();
            }
            else if (!string.IsNullOrWhiteSpace(user.FirstNameEn) || !string.IsNullOrWhiteSpace(user.LastNameEn))
            {
                employeeName = $"{user.FirstNameEn} {user.MiddleNameEn} {user.LastNameEn}".Trim();
            }

            // Generate entries for all days in the period
            for (var date = actualStartDate; date <= actualEndDate; date = date.AddDays(1))
            {
                var key = new { UserId = user.Id, Date = date };
                
                if (attendanceDict.TryGetValue(key, out var attendance))
                {
                    // Track employees with attendance/departure
                    if (attendance.AttendanceTime.HasValue)
                    {
                        employeesWithAttendanceSet.Add(user.Id);
                    }
                    if (attendance.DepartureTime.HasValue)
                    {
                        employeesWithDepartureSet.Add(user.Id);
                    }

                    // Existing attendance record
                    var dayOfWeek = attendance.Date.DayOfWeek;
                    string dayOfWeekName = GetDayOfWeekName(dayOfWeek);

                    results.Add(new AllAttendancesResponse
                    {
                        Id = attendance.Id,
                        EmployeeName = employeeName,
                        MachineCode = user.MachineCode,
                        Date = attendance.Date,
                        DayOfWeek = dayOfWeekName,
                        AttendanceTime = attendance.AttendanceTime,
                        DepartureTime = attendance.DepartureTime,
                        DeviceType = attendance.DeviceType,
                        Location = attendance.Location,
                        LocationId = attendance.LocationId,
                        CreatedAt = attendance.CreatedAt,
                        UpdatedAt = attendance.UpdatedAt
                    });
                }
                else
                {
                    // No attendance record - create entry with null times
                    var dayOfWeek = date.DayOfWeek;
                    string dayOfWeekName = GetDayOfWeekName(dayOfWeek);

                    results.Add(new AllAttendancesResponse
                    {
                        Id = 0, // No database ID for missing records
                        EmployeeName = employeeName,
                        MachineCode = user.MachineCode,
                        Date = date,
                        DayOfWeek = dayOfWeekName,
                        AttendanceTime = null,
                        DepartureTime = null,
                        DeviceType = null,
                        Location = null,
                        LocationId = null,
                        CreatedAt = DateTime.MinValue, // Use MinValue to indicate no record
                        UpdatedAt = null
                    });
                }
            }
        }

        // Apply isCheckIn filter if specified
        if (isCheckIn.HasValue)
        {
            if (isCheckIn.Value)
            {
                // Filter: show only records with AttendanceTime != null
                results = results.Where(r => r.AttendanceTime.HasValue).ToList();
            }
            else
            {
                // Filter: show only records with DepartureTime != null
                results = results.Where(r => r.DepartureTime.HasValue).ToList();
            }
        }

        // Sort by date descending, then by attendance time descending, then by employee name
        var sortedResults = results
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.AttendanceTime)
            .ThenBy(x => x.EmployeeName ?? "")
            .ToList();

        var totalCount = sortedResults.Count;
        var pagedResults = sortedResults
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Calculate total days in the period
        var totalDays = (actualEndDate.DayNumber - actualStartDate.DayNumber) + 1;

        return new AllAttendancesWithSummaryResponse
        {
            Attendances = pagedResults,
            TotalEmployees = totalEmployees,
            EmployeesWithAttendance = employeesWithAttendanceSet.Count,
            EmployeesWithDeparture = employeesWithDepartureSet.Count,
            TotalDays = totalDays,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private static decimal CalculateOvertime(TimeOnly? departureTime, TimeOnly? scheduledEnd)
    {
        if (!departureTime.HasValue || !scheduledEnd.HasValue)
            return 0m;

        var diffMinutes = (departureTime.Value.ToTimeSpan() - scheduledEnd.Value.ToTimeSpan()).TotalMinutes;
        if (diffMinutes <= 0)
            return 0m;

        // Keep 2 decimals
        return Math.Round((decimal)(diffMinutes / 60.0), 2, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateShiftDayHours(TimeOnly? attendanceTime, TimeOnly? departureTime)
    {
        if (attendanceTime.HasValue && departureTime.HasValue)
        {
            var hours = (departureTime.Value.ToTimeSpan() - attendanceTime.Value.ToTimeSpan()).TotalHours;
            if (hours > 0)
                return (decimal)hours;
            return 0m;
        }

        if (attendanceTime.HasValue && !departureTime.HasValue)
            return 8m;

        return 0m;
    }

    private static decimal CalculateShiftDayHours(internalEmployee.Data.Entities.Attendance attendance)
        => CalculateShiftDayHours(attendance.AttendanceTime, attendance.DepartureTime);

    private static decimal CalculateShiftOvertimeHours(internalEmployee.Data.Entities.Attendance attendance)
    {
        var hours = CalculateShiftDayHours(attendance);
        if (hours <= 8m)
            return 0m;

        return Math.Round(hours - 8m, 2, MidpointRounding.AwayFromZero);
    }

    private static string GetDayOfWeekName(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Sunday => "الأحد / Sunday",
            DayOfWeek.Monday => "الاثنين / Monday",
            DayOfWeek.Tuesday => "الثلاثاء / Tuesday",
            DayOfWeek.Wednesday => "الأربعاء / Wednesday",
            DayOfWeek.Thursday => "الخميس / Thursday",
            DayOfWeek.Friday => "الجمعة / Friday",
            DayOfWeek.Saturday => "السبت / Saturday",
            _ => dayOfWeek.ToString()
        };
    }

    public async Task<List<AttendanceExportResponse>> GetAttendanceExportDataAsync(int month, int year, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (month < 1 || month > 12)
            throw new InvalidOperationException("Invalid month. Month must be between 1 and 12.");
        if (year < 2000 || year > 2100)
            throw new InvalidOperationException("Invalid year.");

        // Validate that the requested month is not in the future
        var currentDate = DateTime.Now;
        var requestedDate = new DateOnly(year, month, 1);
        var currentMonthStart = new DateOnly(currentDate.Year, currentDate.Month, 1);
        
        if (requestedDate > currentMonthStart)
            throw new InvalidOperationException("لا يمكن طلب تقرير لشهر لم يأت بعد.");

        // Calculate period from day 26 of previous month to day 25 of current month
        // Example: if month=2 (February), period is from 26/1 to 25/2
        var previousMonth = month == 1 ? 12 : month - 1;
        var previousYear = month == 1 ? year - 1 : year;
        var startDate = new DateOnly(previousYear, previousMonth, 26);
        var endDate = new DateOnly(year, month, 25);
        var effectiveEndDate = GetEffectivePeriodEnd(endDate);
        var startDateTime = startDate.ToDateTime(TimeOnly.MinValue);
        var endDateTime = endDate.ToDateTime(TimeOnly.MaxValue);

        // Get all active users
        var users = await _db.Users
            .Where(u => u.IsActive)
            .Include(u => u.WorkSchedule)
            .ToListAsync(ct);

        // Get all attendances for the period
        var attendances = await _db.Attendances
            .Where(a => a.Date >= startDate && a.Date <= endDate)
            .ToListAsync(ct);

        // Get employment modes
        var employmentModeIds = users.Where(u => u.EmploymentModeId.HasValue).Select(u => u.EmploymentModeId!.Value).Distinct().ToList();
        var employmentModes = await _db.EmploymentModes
            .Where(m => employmentModeIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.Name, ct);

        var results = new List<AttendanceExportResponse>();

        foreach (var user in users)
        {
            var userAttendances = attendances.Where(a => a.UserId == user.Id).ToList();
            
            if (!userAttendances.Any() && user.GrossSalary == null)
                continue; // Skip users with no attendance and no salary

            decimal totalHoursWorked = 0m;
            decimal totalLateDeductionHours = 0m;
            decimal totalOvertimeHours = 0m;
            int attendanceDays = 0;
            var absenceDates = new List<DateOnly>();

            // Get employment mode name
            string? modeName = null;
            if (user.EmploymentModeId.HasValue && employmentModes.TryGetValue(user.EmploymentModeId.Value, out var mode))
            {
                modeName = mode;
            }
            var isFullTime = string.IsNullOrWhiteSpace(modeName) ||
                           string.Equals(modeName, "Full Time", StringComparison.OrdinalIgnoreCase);
            var isPartTime = string.Equals(modeName, "Part Time", StringComparison.OrdinalIgnoreCase);
            var isShift = string.Equals(modeName, "Shift", StringComparison.OrdinalIgnoreCase);
            var partTimeWorkDays = isPartTime ? ResolvePartTimeWorkDays(user.WorkSchedule) : null;

            // Calculate total hours worked, overtime, and late deduction
            foreach (var attendance in userAttendances)
            {
                // Count attendance days (days with AttendanceTime or DepartureTime)
                if (attendance.AttendanceTime.HasValue || attendance.DepartureTime.HasValue)
                {
                    attendanceDays++;
                }

                // Get scheduled times for calculations
                var scheduledTimes = await ResolveScheduledTimesByEmploymentModeAsync(user, attendance.Date, ct);
                var scheduledStart = scheduledTimes.Start;
                var scheduledEnd = scheduledTimes.End;

                // Calculate actual hours worked for this day
                decimal dayHours = 0m;
                
                if (attendance.AttendanceTime.HasValue && attendance.DepartureTime.HasValue)
                {
                    // Case 1: Both AttendanceTime and DepartureTime exist - calculate difference
                    var hours = (attendance.DepartureTime.Value.ToTimeSpan() - attendance.AttendanceTime.Value.ToTimeSpan()).TotalHours;
                    // Ensure hours is positive (DepartureTime should be after AttendanceTime)
                    if (hours > 0)
                        dayHours = (decimal)hours;
                    // If hours is negative or zero, dayHours remains 0 (data error)
                }
                else if (attendance.AttendanceTime.HasValue && !attendance.DepartureTime.HasValue)
                {
                    // Case 2: Only AttendanceTime exists - calculate from attendance time to scheduled end (or default 17:00)
                    var endTime = scheduledEnd ?? DefaultFullTimeEnd;
                    var hours = (endTime.ToTimeSpan() - attendance.AttendanceTime.Value.ToTimeSpan()).TotalHours;
                    if (hours > 0)
                        dayHours = (decimal)hours;
                }
                else if (!attendance.AttendanceTime.HasValue && attendance.DepartureTime.HasValue)
                {
                    // Case 3: Only DepartureTime exists - calculate from scheduled start (or default 09:00) to departure time
                    var startTime = scheduledStart ?? DefaultFullTimeStart;
                    var hours = (attendance.DepartureTime.Value.ToTimeSpan() - startTime.ToTimeSpan()).TotalHours;
                    if (hours > 0)
                        dayHours = (decimal)hours;
                }

                if (isShift)
                {
                    dayHours = CalculateShiftDayHours(attendance);
                }

                totalHoursWorked += dayHours;

                // Calculate overtime hours
                var overtimeHours = isShift
                    ? CalculateShiftOvertimeHours(attendance)
                    : CalculateOvertime(attendance.DepartureTime, scheduledEnd);
                totalOvertimeHours += overtimeHours;

                // Calculate late deduction hours
                var (_, _, lateHours) = CalculateLateDeduction(attendance.AttendanceTime, scheduledStart);
                totalLateDeductionHours += lateHours;
            }

            // Calculate absences: days in period without attendance records
            // Full Time: Sunday-Thursday are work days (Friday/Saturday are weekends)
            // Part Time: either default Sunday-Thursday or custom selected work days
            // Shift: absences are not calculated here (paid only by attendance)
            for (var date = startDate; date <= effectiveEndDate; date = date.AddDays(1))
            {
                var dayOfWeek = date.DayOfWeek;
                var hasAttendance = userAttendances.Any(a => a.Date == date);

                if (!hasAttendance)
                {
                    bool isWorkDay = false;

                    if (isFullTime)
                    {
                        isWorkDay = IsFullTimeWeekday(dayOfWeek);
                    }
                    else if (isPartTime)
                    {
                        isWorkDay = partTimeWorkDays != null && partTimeWorkDays.Contains(dayOfWeek);
                    }

                    if (isWorkDay)
                    {
                        absenceDates.Add(date);
                    }
                }
            }

            // Build employee name
            string employeeName = string.Empty;
            if (!string.IsNullOrWhiteSpace(user.FirstNameAr) || !string.IsNullOrWhiteSpace(user.LastNameAr))
            {
                employeeName = $"{user.FirstNameAr} {user.MiddleNameAr} {user.LastNameAr}".Trim();
            }
            else if (!string.IsNullOrWhiteSpace(user.FirstNameEn) || !string.IsNullOrWhiteSpace(user.LastNameEn))
            {
                employeeName = $"{user.FirstNameEn} {user.MiddleNameEn} {user.LastNameEn}".Trim();
            }

            results.Add(new AttendanceExportResponse
            {
                EmployeeName = employeeName,
                GrossSalary = user.GrossSalary ?? 0m,
                AttendanceDays = attendanceDays,
                TotalHoursWorked = Math.Round(totalHoursWorked, 2),
                AbsenceDays = absenceDates.Count,
                EmploymentModeName = modeName,
                TotalOvertimeHours = Math.Round(totalOvertimeHours, 2),
                TotalLateDeductionHours = Math.Round(totalLateDeductionHours, 2),
                AbsenceDates = absenceDates.Any() ? string.Join(", ", absenceDates.Select(d => d.ToString("yyyy-MM-dd"))) : null
            });
        }

        return results.OrderBy(r => r.EmployeeName).ToList();
    }

    private static (double Latitude, double Longitude) ValidateMobileLocation(double? latitude, double? longitude)
    {
        if (!latitude.HasValue || !longitude.HasValue)
            throw new InvalidOperationException("برجاء تفعيل تحديد الموقع (GPS) في الموبايل قبل البصمة.");

        if (latitude.Value is < -90 or > 90 || longitude.Value is < -180 or > 180)
            throw new InvalidOperationException("إحداثيات الموقع غير صحيحة.");

        return (latitude.Value, longitude.Value);
    }

    private bool IsWithinCompanyLocation(double latitude, double longitude, int? radiusOverride)
    {
        if (_companyLocation.Latitude == 0 && _companyLocation.Longitude == 0)
            return false;

        var distance = CalculateDistanceMeters(latitude, longitude, _companyLocation.Latitude, _companyLocation.Longitude);
        var radius = radiusOverride.HasValue && radiusOverride.Value > 0
            ? radiusOverride.Value
            : _companyLocation.RadiusMeters;
        return distance <= radius;
    }

    private static string FormatMobileLocationEntry(string label, double latitude, double longitude)
        => FormattableString.Invariant($"{label}: {latitude:F6},{longitude:F6}");

    private static string MergeMobileLocationEntry(string? currentLocation, string newEntry)
    {
        if (string.IsNullOrWhiteSpace(currentLocation))
            return newEntry;

        var entries = currentLocation
            .Split(" | ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(entry => !entry.StartsWith(newEntry.Split(':')[0], StringComparison.OrdinalIgnoreCase))
            .ToList();

        entries.Add(newEntry);
        return string.Join(" | ", entries);
    }

    private async Task<int?> ResolveLocationIdOrThrowAsync(
        Guid userId,
        double latitude,
        double longitude,
        int? radiusOverride,
        bool allowFromAnyLocation,
        CancellationToken ct)
    {
        // 0) Check if user has an approved Work From Home request for today
        var today = DateTime.Now.Date;
        var hasApprovedWFH = await _db.WorkFromHomeRequests
            .AnyAsync(r => r.UserId == userId && 
                          r.Status == RequestStatus.Approved && 
                          r.StartDate <= today && 
                          r.EndDate >= today, ct);

        if (hasApprovedWFH)
            return null; // Allowed from anywhere

        // 0.1) Check if user has an approved Mission (Assignment) for today
        var hasApprovedAssignment = await _db.Assignments
            .AnyAsync(a => a.UserId == userId && 
                          a.Status == RequestStatus.Approved && 
                          a.StartDate <= today && 
                          a.EndDate >= today, ct);

        if (hasApprovedAssignment)
            return null; // Allowed from anywhere

        // 1) Check company location first
        if (IsWithinCompanyLocation(latitude, longitude, radiusOverride))
            return null;

        // 2) Then check user-specific allowed locations
        var locations = await _db.UserLocations
            .Where(l => l.UserId == userId && l.IsActive)
            .ToListAsync(ct);

        if (locations.Count == 0)
        {
            if (allowFromAnyLocation)
                return null;

            throw new InvalidOperationException("أنت خارج مقر الشركة ولا توجد مواقع مسموح بها لهذا المستخدم.");
        }

        foreach (var loc in locations)
        {
            var distance = CalculateDistanceMeters(latitude, longitude, loc.Latitude, loc.Longitude);
            if (distance <= loc.RadiusMeters)
                return loc.Id;
        }

        if (allowFromAnyLocation)
            return null;

        throw new InvalidOperationException("أنت خارج مقر الشركة وخارج المواقع المسموح بها لك.");
    }

    private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula
        var radLat1 = DegreesToRadians(lat1);
        var radLat2 = DegreesToRadians(lat2);
        var deltaLat = DegreesToRadians(lat2 - lat1);
        var deltaLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(radLat1) * Math.Cos(radLat2) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180d);

    private static string? GetEmployeeStatusNote(AppUser user, DateOnly periodStartDate, DateOnly periodEndDate)
    {
        if (user.StartDate.HasValue && periodEndDate < user.StartDate.Value)
            return "لم يكن الموظف على رأس العمل خلال هذه الفترة (قبل تاريخ التعيين).";

        if (user.ContractEndDate.HasValue && periodStartDate > user.ContractEndDate.Value)
            return "انتهى عقد الموظف قبل بداية هذه الفترة.";

        return null;
    }

    public async Task<CalculateSalaryResponse> CalculateSalaryAsync(CalculateSalaryRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (request.Month < 1 || request.Month > 12)
            throw new InvalidOperationException("Invalid month. Month must be between 1 and 12.");
        if (request.Year < 2000 || request.Year > 2100)
            throw new InvalidOperationException("Invalid year.");

        // Get employee
        var user = await _db.Users
            .Where(u => u.Id == request.EmployeeId) // Support calculating salary for inactive users who worked part of the month
            .Include(u => u.WorkSchedule)
            .FirstOrDefaultAsync(ct);

        if (user == null)
            throw new InvalidOperationException("الموظف غير موجود.");

        // If GrossSalary is missing, treat it as zero instead of failing.

        // Calculate period from day 26 of previous month to day 25 of current month
        var previousMonth = request.Month == 1 ? 12 : request.Month - 1;
        var previousYear = request.Month == 1 ? request.Year - 1 : request.Year;
        var startDate = new DateOnly(previousYear, previousMonth, 26);
        var endDate = new DateOnly(request.Year, request.Month, 25);
        var startDateTime = startDate.ToDateTime(TimeOnly.MinValue);
        var endDateTime = endDate.ToDateTime(TimeOnly.MaxValue);

        var employeeStatusNote = GetEmployeeStatusNote(user, startDate, endDate);
        if (employeeStatusNote != null)
        {
            return new CalculateSalaryResponse
            {
                EmployeeStatusNote = employeeStatusNote,
                InsuranceSalary = user.InsuranceSalary,
                ShiftRate = user.ShiftRate
            };
        }

        // 1. Get all attendances for the period
        var attendances = await _db.Attendances
            .Where(a => a.UserId == user.Id && a.Date >= startDate && a.Date <= endDate)
            .ToListAsync(ct);

        // 2. Get approved leaves for the period
        var approvedLeaves = await _db.Leaves
            .Where(l => l.UserId == user.Id && l.Status == RequestStatus.Approved &&
                       l.StartDate <= endDateTime && l.EndDate >= startDateTime)
            .ToListAsync(ct);

        // 3. Get approved permissions for the period
        var approvedPermissions = await _db.Permissions
            .Where(p => p.UserId == user.Id && p.Status == RequestStatus.Approved &&
                       p.Date >= startDateTime && p.Date <= endDateTime)
            .ToListAsync(ct);

        // 4. Get approved assignments for the period
        var approvedAssignments = await _db.Assignments
            .Where(a => a.UserId == user.Id && a.Status == RequestStatus.Approved &&
                       a.StartDate <= endDateTime && a.EndDate >= startDateTime)
            .ToListAsync(ct);
        
        // 5. Get approved overtime requests for the period
        var approvedOvertimes = await _db.Overtimes
            .Where(o => o.UserId == user.Id && o.Status == RequestStatus.Approved &&
                       o.StartTime >= startDateTime && o.StartTime <= endDateTime)
            .ToListAsync(ct);

        // Get employment mode name
        string? modeName = null;
        if (user.EmploymentModeId.HasValue)
        {
            modeName = await _db.EmploymentModes
                .AsNoTracking()
                .Where(m => m.Id == user.EmploymentModeId.Value)
                .Select(m => m.Name)
                .FirstOrDefaultAsync(ct);
        }
        var isFullTime = string.IsNullOrWhiteSpace(modeName) || 
                       string.Equals(modeName, "Full Time", StringComparison.OrdinalIgnoreCase);
        var isPartTime = string.Equals(modeName, "Part Time", StringComparison.OrdinalIgnoreCase);
        var isShift = string.Equals(modeName, "Shift", StringComparison.OrdinalIgnoreCase);
        var partTimeWorkDays = isPartTime ? ResolvePartTimeWorkDays(user.WorkSchedule) : null;

        decimal totalHoursWorked = 0m;
        decimal totalLateDeductionHours = 0m;
        decimal totalOvertimeHours = 0m;
        decimal overtimePay = 0m;
        int attendanceDays = 0;
        var absenceDates = new List<DateOnly>();
        var lateDates = new List<DateOnly>();
        int totalWorkingDays = 0;
        int displayTotalWorkingDays = 0;
        var displayPaidDays = new HashSet<DateOnly>(
            attendances
                .Where(a => a.AttendanceTime.HasValue || a.DepartureTime.HasValue)
                .Select(a => a.Date));

        // Calculate total hours worked, overtime, and late deduction
        foreach (var attendance in attendances)
        {
            // Count attendance days (days with AttendanceTime or DepartureTime)
            if (attendance.AttendanceTime.HasValue || attendance.DepartureTime.HasValue)
            {
                attendanceDays++;
            }

            // Get scheduled times for calculations
            var scheduledTimes = await ResolveScheduledTimesByEmploymentModeAsync(user, attendance.Date, ct);
            var scheduledStart = scheduledTimes.Start;
            var scheduledEnd = scheduledTimes.End;

            // Calculate actual hours worked for this day
            decimal dayHours = 0m;
            
            if (attendance.AttendanceTime.HasValue && attendance.DepartureTime.HasValue)
            {
                // Case 1: Both AttendanceTime and DepartureTime exist - calculate difference
                var hours = (attendance.DepartureTime.Value.ToTimeSpan() - attendance.AttendanceTime.Value.ToTimeSpan()).TotalHours;
                if (hours > 0)
                    dayHours = (decimal)hours;
            }
            else if (attendance.AttendanceTime.HasValue && !attendance.DepartureTime.HasValue)
            {
                // Case 2: Only AttendanceTime exists - calculate from attendance time to scheduled end (or default 17:00)
                var endTime = scheduledEnd ?? DefaultFullTimeEnd;
                var hours = (endTime.ToTimeSpan() - attendance.AttendanceTime.Value.ToTimeSpan()).TotalHours;
                if (hours > 0)
                    dayHours = (decimal)hours;
            }
            else if (!attendance.AttendanceTime.HasValue && attendance.DepartureTime.HasValue)
            {
                // Case 3: Only DepartureTime exists - calculate from scheduled start (or default 09:00) to departure time
                var startTime = scheduledStart ?? DefaultFullTimeStart;
                var hours = (attendance.DepartureTime.Value.ToTimeSpan() - startTime.ToTimeSpan()).TotalHours;
                if (hours > 0)
                    dayHours = (decimal)hours;
            }

            if (isShift)
            {
                dayHours = CalculateShiftDayHours(attendance);
            }

            totalHoursWorked += dayHours;

            // Calculate overtime hours
            var overtimeHours = isShift
                ? CalculateShiftOvertimeHours(attendance)
                : CalculateOvertime(attendance.DepartureTime, scheduledEnd);
            totalOvertimeHours += overtimeHours;

            // Calculate late deduction hours
            var (_, _, lateHours) = CalculateLateDeduction(attendance.AttendanceTime, scheduledStart);
            
            // If there is any approved permission or assignment for this date, cancel late deduction for the day
            if (lateHours > 0)
            {
                var dayPermissions = approvedPermissions
                    .Where(p => p.Date.Date == attendance.Date.ToDateTime(TimeOnly.MinValue).Date)
                    .ToList();

                var hasApprovedAssignment = approvedAssignments.Any(a =>
                    attendance.Date >= DateOnly.FromDateTime(a.StartDate) &&
                    attendance.Date <= DateOnly.FromDateTime(a.EndDate));

                var hasApprovedLeave = approvedLeaves.Any(l =>
                    attendance.Date >= DateOnly.FromDateTime(l.StartDate) &&
                    attendance.Date <= DateOnly.FromDateTime(l.EndDate));

                if (dayPermissions.Any() || hasApprovedAssignment || hasApprovedLeave)
                {
                    lateHours = 0m;
                }
            }

            if (lateHours > 0)
                lateDates.Add(attendance.Date);
            
            totalLateDeductionHours += lateHours;
        }

        // Override overtime hours with approved overtime requests (paid only if approved)
        if (isShift)
        {
            if (approvedOvertimes.Count > 0)
            {
                totalOvertimeHours = approvedOvertimes.Sum(o => o.TotalHours);
                var shiftRate = user.ShiftRate ?? 0m;
                overtimePay = (shiftRate / 8m) * totalOvertimeHours;
            }
            else
            {
                totalOvertimeHours = 0m;
                overtimePay = 0m;
            }
        }
        else if (approvedOvertimes.Count > 0)
        {
            totalOvertimeHours = approvedOvertimes.Sum(o => o.TotalHours);
            overtimePay = approvedOvertimes.Sum(o => o.Amount);
        }
        else
        {
            totalOvertimeHours = 0m;
            overtimePay = 0m;
        }

        // Calculate absences and work days
        var effectiveStartDateForLoop = user.StartDate.HasValue && user.StartDate.Value > startDate ? user.StartDate.Value : startDate;
        var effectiveEndDateForLoop = user.ContractEndDate.HasValue && user.ContractEndDate.Value < endDate ? user.ContractEndDate.Value : endDate;

        for (var date = effectiveStartDateForLoop; date <= effectiveEndDateForLoop; date = date.AddDays(1))
        {
            var dayOfWeek = date.DayOfWeek;
            displayTotalWorkingDays++;

            var isPublicHolidayForDisplay = await IsPublicHolidayForUserAsync(date, user.Id, user.DepartmentId, user.EmploymentModeId, user.Religion, ct);
            var isWeekend = dayOfWeek == DayOfWeek.Friday || dayOfWeek == DayOfWeek.Saturday;
            if (isWeekend || isPublicHolidayForDisplay)
            {
                displayPaidDays.Add(date);
            }
            
            // Check if it's a work day
            bool isWorkDay = false;
            if (isFullTime)
            {
                isWorkDay = IsFullTimeWeekday(dayOfWeek);
            }
            else if (isPartTime)
            {
                isWorkDay = partTimeWorkDays != null && partTimeWorkDays.Contains(dayOfWeek);
            }
            else if (isShift)
            {
                isWorkDay = false;
            }

            if (isWorkDay)
            {
                totalWorkingDays++;
                
                var hasAttendance = attendances.Any(a => a.Date == date && (a.AttendanceTime.HasValue || a.DepartureTime.HasValue));
                if (!hasAttendance)
                {
                    // Check if covered by approved leave
                    var hasApprovedLeave = approvedLeaves.Any(l => date >= DateOnly.FromDateTime(l.StartDate) && date <= DateOnly.FromDateTime(l.EndDate));
                    
                    // Check if covered by approved assignment
                    var hasApprovedAssignment = approvedAssignments.Any(a => date >= DateOnly.FromDateTime(a.StartDate) && date <= DateOnly.FromDateTime(a.EndDate));
                    
                    // Check if it's a public holiday
                    var isPublicHoliday = await IsPublicHolidayForUserAsync(date, user.Id, user.DepartmentId, user.EmploymentModeId, user.Religion, ct);
                    
                    if (!hasApprovedLeave && !hasApprovedAssignment && !isPublicHoliday)
                    {
                        absenceDates.Add(date);
                    }
                }
            }
        }

        // Calculate actual employment days within the period (inclusive of weekends)
        int totalEmploymentDaysInRange = 0;
        for (var empDate = effectiveStartDateForLoop; empDate <= effectiveEndDateForLoop; empDate = empDate.AddDays(1))
        {
            totalEmploymentDaysInRange++;
        }

        // Calculate deductions
        var paidShiftDays = isShift
            ? attendances.Where(a => a.AttendanceTime.HasValue).Select(a => a.Date).Distinct().Count()
            : totalWorkingDays - absenceDates.Count;
        if (paidShiftDays < 0) paidShiftDays = 0;

        var displayPaidShiftDays = isShift
            ? displayPaidDays.Count
            : displayTotalWorkingDays - absenceDates.Count;
        if (displayPaidShiftDays < 0) displayPaidShiftDays = 0;

        var shiftWorkedRegularHours = isShift
            ? attendances
                .Where(a => a.AttendanceTime.HasValue || a.DepartureTime.HasValue)
                .Sum(a => CalculateShiftDayHours(a))
            : 0m;

        var shiftHourlyRate = (user.ShiftRate ?? 0m) / 8m;
        var fullMonthlySalary = user.GrossSalary ?? 0m;
        var baseSalary = isShift
            ? shiftHourlyRate * shiftWorkedRegularHours
            : (fullMonthlySalary * (totalEmploymentDaysInRange / 30m));

        var dailySalary = fullMonthlySalary / 30m;

        // FIXED LOGIC: If the employee missed ALL working days (without excuses), they lose the whole salary (including weekends)
        decimal absenceDeductionAmount = 0m;
        if (!isShift)
        {
            if (absenceDates.Count > 0 && absenceDates.Count == totalWorkingDays && totalWorkingDays > 0)
            {
                absenceDeductionAmount = baseSalary; // Whole month deduction
            }
            else
            {
                absenceDeductionAmount = absenceDates.Count * dailySalary;
            }
        }

        var lateDeductionAmount = (totalLateDeductionHours / 8m) * dailySalary;
        var advanceDeductionAmount = await CalculateSalaryAdvanceDeductionAsync(user.Id, endDate, ct);
        var healthInsuranceAmount = await CalculateHealthInsuranceDeductionAsync(user.Id, startDate, endDate, ct);
        var totalDeductions = lateDeductionAmount + absenceDeductionAmount + advanceDeductionAmount + healthInsuranceAmount;

        // Overtime pay is calculated from approved overtime requests

        // Calculate insurance and tax
        decimal employeeInsuranceAmount = 0m;
        decimal companyInsuranceAmount = 0m;
        if (user.IsInsured)
        {
            // Use InsuranceSalary if provided, otherwise fallback to GrossSalary
            var insuranceBase = user.InsuranceSalary ?? baseSalary;
            var (empIns, compIns) = await CalculateInsuranceAsync(insuranceBase, ct);
            employeeInsuranceAmount = empIns;
            companyInsuranceAmount = compIns;
        }
        
        decimal taxAmount = user.WorkEarningsTax ?? 0m;

        // Calculate penalties
        var (penaltyDeductionAmount, appliedPenalties) = await CalculatePenaltiesAsync(user.Id, baseSalary, request.Month, request.Year, ct);

        // Calculate Bonuses for the period
        var bonuses = await _db.EmployeeBonuses
            .Where(b => b.UserId == user.Id && b.BonusDate >= startDate && b.BonusDate <= endDate)
            .ToListAsync(ct);
        var totalBonusAmount = bonuses.Sum(b => b.Amount);

        // Fetch allowances from user if they have values, otherwise default to 0
        var housingAllowance = user.HousingAllowance ?? 0m;
        var mealAllowance = user.MealAllowance ?? 0m;
        var transportationAllowance = user.TransportationAllowance ?? 0m;
        var insuranceAllowance = user.InsuranceAllowance ?? 0m;
        
        // Calculate net salary
        var totalAllowances = housingAllowance + mealAllowance + transportationAllowance + insuranceAllowance;
        var totalEarnings = baseSalary + totalAllowances + overtimePay + totalBonusAmount;
        var salaryBeforeInsuranceAndTax = totalEarnings - totalDeductions - penaltyDeductionAmount;

        // If employee missed all working days OR has zero attendance, final salary results should be zero
        if ((totalWorkingDays > 0 && absenceDates.Count == totalWorkingDays) || attendanceDays == 0)
        {
            salaryBeforeInsuranceAndTax = 0m;
        }
        
        // Ensure salary doesn't go below zero
        if (salaryBeforeInsuranceAndTax < 0) salaryBeforeInsuranceAndTax = 0;
        
        var netSalary = salaryBeforeInsuranceAndTax - employeeInsuranceAmount - taxAmount;
        if (netSalary < 0) netSalary = 0;

        return new CalculateSalaryResponse
        {
            GrossSalary = Math.Round(baseSalary, 2), // Used as 'Base Salary' in payslip
            TotalEarnings = Math.Round(totalEarnings, 2), // Total before deductions
            NetSalary = Math.Round(netSalary, 2),
            EmployeeStatusNote = null,
            BonusAmount = Math.Round(totalBonusAmount, 2),
            TaxAmount = Math.Round(taxAmount, 2),
            InsuranceSalary = user.InsuranceSalary,
            
            Allowances = new AllowancesInfo
            {
                Housing = Math.Round(housingAllowance, 2),
                Meal = Math.Round(mealAllowance, 2),
                Transportation = Math.Round(transportationAllowance, 2),
                Insurance = Math.Round(insuranceAllowance, 2),
                Other = 0m // Placeholder for future use
            },
            
            Deductions = new DeductionsInfo
            {
                LateAmount = Math.Round(lateDeductionAmount, 2),
                LateHours = Math.Round(totalLateDeductionHours, 2),
                AbsenceAmount = Math.Round(absenceDeductionAmount, 2),
                AbsenceDays = absenceDates.Count,
                PenaltiesAmount = Math.Round(penaltyDeductionAmount, 2),
                AdvancesAmount = Math.Round(advanceDeductionAmount, 2),
                HealthInsuranceAmount = Math.Round(healthInsuranceAmount, 2),
                PenaltyDetails = appliedPenalties.Select(p => new PenaltyInfo 
                { 
                    Id = p.Id, 
                    PenaltyType = p.PenaltyType, 
                    Amount = p.Amount, 
                    Days = p.Days, 
                    PenaltyDate = p.PenaltyDate, 
                    Reason = p.Reason 
                }).ToList()
            },
            
            Insurance = new InsuranceInfo
            {
                Social = Math.Round(employeeInsuranceAmount, 2),
                Health = 0m, // Split logic could be added here if needed
                CompanyShare = Math.Round(companyInsuranceAmount, 2),
                InsuranceSalary = user.IsInsured ? (user.InsuranceSalary ?? baseSalary) : 0m
            },
            
            ShiftRate = user.ShiftRate,
            PaidShiftDays = displayPaidShiftDays,
            TotalWorkingDays = displayTotalWorkingDays,
            AbsenceDates = absenceDates.Select(d => d.ToString("yyyy-MM-dd")).ToList(),
            LateDates = lateDates.Select(d => d.ToString("yyyy-MM-dd")).ToList(),
            HoursWorked = Math.Round(totalHoursWorked, 2),
            OvertimeHours = Math.Round(totalOvertimeHours, 2),
            OvertimePay = Math.Round(overtimePay, 2)
        };
    }

    public async Task<List<MonthlyReportResponse>> GetMonthlyReportForAllEmployeesAsync(int month, int year, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (month < 1 || month > 12)
            throw new InvalidOperationException("Invalid month. Month must be between 1 and 12.");
        if (year < 2000 || year > 2100)
            throw new InvalidOperationException("Invalid year.");

        // Validate that the requested month is not in the future
        var currentDate = DateTime.Now;
        var requestedDate = new DateOnly(year, month, 1);
        var currentMonthStart = new DateOnly(currentDate.Year, currentDate.Month, 1);
        
        if (requestedDate > currentMonthStart)
            throw new InvalidOperationException("لا يمكن طلب تقرير لشهر لم يأت بعد.");

        // Calculate period: from 26th of previous month to 25th of current month
        var previousMonth = month == 1 ? 12 : month - 1;
        var previousYear = month == 1 ? year - 1 : year;
        var startDate = new DateOnly(previousYear, previousMonth, 26);
        var endDate = new DateOnly(year, month, 25);
        var effectiveEndDate = GetEffectivePeriodEnd(endDate);
        var startDateTime = startDate.ToDateTime(TimeOnly.MinValue);
        var endDateTime = endDate.ToDateTime(TimeOnly.MaxValue);

        // Get all active employees (IsActive = true)
        var users = await _db.Users
            .AsNoTracking()
            .Include(u => u.WorkSchedule)
            .Where(u => u.IsActive)
            .ToListAsync(ct);

        // Get all attendances for the month
        var allAttendances = await _db.Attendances
            .AsNoTracking()
            .Where(a => a.Date >= startDate && a.Date <= endDate)
            .ToListAsync(ct);

        // Get all approved leaves overlapping the period
        var allLeaves = await _db.Leaves
            .AsNoTracking()
            .Where(l => l.Status == RequestStatus.Approved &&
                       l.StartDate <= endDateTime &&
                       l.EndDate >= startDateTime)
            .ToListAsync(ct);

        // Get all approved assignments overlapping the period
        var allAssignments = await _db.Assignments
            .AsNoTracking()
            .Where(a => a.Status == RequestStatus.Approved &&
                       a.StartDate <= endDateTime &&
                       a.EndDate >= startDateTime)
            .ToListAsync(ct);

        // Get departments for mapping
        var departmentIds = users.Where(u => u.DepartmentId.HasValue).Select(u => u.DepartmentId!.Value).Distinct().ToList();
        var departments = await _db.Departments
            .AsNoTracking()
            .Where(d => departmentIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Name, ct);

        // Get employment modes for mapping
        var employmentModeIds = users.Where(u => u.EmploymentModeId.HasValue).Select(u => u.EmploymentModeId!.Value).Distinct().ToList();
        var employmentModes = await _db.EmploymentModes
            .AsNoTracking()
            .Where(m => employmentModeIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.Name, ct);

        // Get job titles for mapping
        var jobIdsMonthly = users.Where(u => u.JobId.HasValue).Select(u => u.JobId!.Value).Distinct().ToList();
        var jobTitlesMonthly = await _db.JobTitles
            .AsNoTracking()
            .Where(j => jobIdsMonthly.Contains(j.Id))
            .ToDictionaryAsync(j => j.Id, j => j.Name, ct);

        var results = new List<MonthlyReportResponse>();

        foreach (var user in users)
        {
            var userAttendances = allAttendances.Where(a => a.UserId == user.Id).ToList();
            var userLeaves = allLeaves.Where(l => l.UserId == user.Id).ToList();
            var userAssignments = allAssignments.Where(a => a.UserId == user.Id).ToList();

            // Get employment mode
            string? modeName = null;
            if (user.EmploymentModeId.HasValue && employmentModes.TryGetValue(user.EmploymentModeId.Value, out var mode))
            {
                modeName = mode;
            }
            var isFullTime = string.IsNullOrWhiteSpace(modeName) ||
                           string.Equals(modeName, "Full Time", StringComparison.OrdinalIgnoreCase);
            var isPartTime = string.Equals(modeName, "Part Time", StringComparison.OrdinalIgnoreCase);
            var isShift = string.Equals(modeName, "Shift", StringComparison.OrdinalIgnoreCase);
            var partTimeWorkDays = isPartTime ? ResolvePartTimeWorkDays(user.WorkSchedule) : null;

            // Calculate TotalAttendance
            var totalAttendance = userAttendances.Count(a => a.AttendanceTime.HasValue || a.DepartureTime.HasValue);

            // Calculate TotalAbsence
            var absenceDates = new List<DateOnly>();
            for (var date = startDate; date <= effectiveEndDate; date = date.AddDays(1))
            {
                var dayOfWeek = date.DayOfWeek;
                var hasAttendance = userAttendances.Any(a => a.Date == date && (a.AttendanceTime.HasValue || a.DepartureTime.HasValue));

                if (!hasAttendance)
                {
                    bool isWorkDay = false;

                    if (isFullTime)
                    {
                        isWorkDay = IsFullTimeWeekday(dayOfWeek);
                    }
                    else if (isPartTime)
                    {
                        isWorkDay = partTimeWorkDays != null && partTimeWorkDays.Contains(dayOfWeek);
                    }
                    else if (isShift)
                    {
                        isWorkDay = false;
                    }

                    if (isWorkDay)
                    {
                        // Check if it's a public holiday for this user
                        var isPublicHoliday = await IsPublicHolidayForUserAsync(date, user.Id, user.DepartmentId, user.EmploymentModeId, user.Religion, ct);
                        var hasApprovedLeave = userLeaves.Any(l => date >= DateOnly.FromDateTime(l.StartDate) && date <= DateOnly.FromDateTime(l.EndDate));
                        var hasApprovedAssignment = userAssignments.Any(a => date >= DateOnly.FromDateTime(a.StartDate) && date <= DateOnly.FromDateTime(a.EndDate));

                        if (!isPublicHoliday && !hasApprovedLeave && !hasApprovedAssignment)
                            absenceDates.Add(date);
                    }
                }
            }
            var totalAbsence = absenceDates.Count;

            // Calculate leaves (Annual, Casual, Sick) - only for leaves in this month
            decimal annualLeave = 0m;
            decimal casualLeave = 0m;
            decimal sickLeave = 0m;

            foreach (var leave in userLeaves)
            {
                var leaveStart = DateOnly.FromDateTime(leave.StartDate);
                var leaveEnd = DateOnly.FromDateTime(leave.EndDate);

                // Only count days that fall within the report month
                var reportStart = leaveStart > startDate ? leaveStart : startDate;
                var reportEnd = leaveEnd < endDate ? leaveEnd : endDate;

                if (reportStart <= reportEnd)
                {
                    var days = CalculateWorkingDaysForLeave(reportStart, reportEnd);

                    switch (leave.LeaveType)
                    {
                        case LeaveType.Annual:
                            annualLeave += days;
                            break;
                        case LeaveType.Casual:
                            casualLeave += days;
                            break;
                        case LeaveType.Sick:
                            sickLeave += days;
                            break;
                    }
                }
            }

            // Calculate TotalWorkedHoursInMonth
            decimal totalWorkedHours = 0m;
            decimal totalLateDeductionHours = 0m;
            decimal totalOvertimeHours = 0m;

            foreach (var attendance in userAttendances)
            {
                var scheduledTimes = await ResolveScheduledTimesByEmploymentModeAsync(user, attendance.Date, ct);
                var scheduledStart = scheduledTimes.Start;
                var scheduledEnd = scheduledTimes.End;

                // Calculate actual hours worked for this day
                decimal dayHours = 0m;

                if (attendance.AttendanceTime.HasValue && attendance.DepartureTime.HasValue)
                {
                    var hours = (attendance.DepartureTime.Value.ToTimeSpan() - attendance.AttendanceTime.Value.ToTimeSpan()).TotalHours;
                    if (hours > 0)
                        dayHours = (decimal)hours;
                }
                else if (attendance.AttendanceTime.HasValue && !attendance.DepartureTime.HasValue)
                {
                    var endTime = scheduledEnd ?? DefaultFullTimeEnd;
                    var hours = (endTime.ToTimeSpan() - attendance.AttendanceTime.Value.ToTimeSpan()).TotalHours;
                    if (hours > 0)
                        dayHours = (decimal)hours;
                }
                else if (!attendance.AttendanceTime.HasValue && attendance.DepartureTime.HasValue)
                {
                    var startTime = scheduledStart ?? DefaultFullTimeStart;
                    var hours = (attendance.DepartureTime.Value.ToTimeSpan() - startTime.ToTimeSpan()).TotalHours;
                    if (hours > 0)
                        dayHours = (decimal)hours;
                }

                totalWorkedHours += dayHours;

                // Calculate overtime hours
                var overtimeHours = isShift
                    ? CalculateShiftOvertimeHours(attendance)
                    : CalculateOvertime(attendance.DepartureTime, scheduledEnd);
                totalOvertimeHours += overtimeHours;

                // Calculate late deduction hours
                var (_, _, lateHours) = CalculateLateDeduction(attendance.AttendanceTime, scheduledStart);
                totalLateDeductionHours += lateHours;
            }

            // Calculate TotalHours (required hours based on work schedule)
            decimal totalHours = 0m;
            int totalWorkingDays = 0;
            for (var date = startDate; date <= effectiveEndDate; date = date.AddDays(1))
            {
                var dayOfWeek = date.DayOfWeek;
                bool isWorkDay = false;
                TimeOnly? startTime = null;
                TimeOnly? endTime = null;

                if (isFullTime)
                {
                    if (IsFullTimeWeekday(dayOfWeek))
                    {
                        isWorkDay = true;
                        startTime = user.WorkSchedule?.FullTimeStartOverride ?? DefaultFullTimeStart;
                        endTime = user.WorkSchedule?.FullTimeEndOverride ?? DefaultFullTimeEnd;
                    }
                }
                else if (isPartTime)
                {
                    if (partTimeWorkDays != null &&
                        partTimeWorkDays.Contains(dayOfWeek) &&
                        user.WorkSchedule?.PartTimeStart.HasValue == true &&
                        user.WorkSchedule?.PartTimeEnd.HasValue == true)
                    {
                        isWorkDay = true;
                        startTime = user.WorkSchedule.PartTimeStart;
                        endTime = user.WorkSchedule.PartTimeEnd;
                    }
                }
                else if (isShift)
                {
                    isWorkDay = false;
                }

                if (isWorkDay && startTime.HasValue && endTime.HasValue)
                {
                    totalWorkingDays++;
                    var hours = (endTime.Value.ToTimeSpan() - startTime.Value.ToTimeSpan()).TotalHours;
                    if (hours > 0)
                        totalHours += (decimal)hours;
                }
            }

            // Align salary figures with CalculateSalaryAsync / Payslip
            var salaryDetails = await CalculateSalaryAsync(new CalculateSalaryRequest
            {
                EmployeeId = user.Id,
                Month = month,
                Year = year
            }, ct);

            if (salaryDetails.EmployeeStatusNote != null)
            {
                totalAttendance = 0;
                totalAbsence = 0;
                annualLeave = 0m;
                casualLeave = 0m;
                sickLeave = 0m;
                totalWorkedHours = 0m;
                totalLateDeductionHours = 0m;
                totalOvertimeHours = 0m;
                totalHours = 0m;
            }

            totalAbsence = salaryDetails.Deductions.AbsenceDays;
            totalLateDeductionHours = salaryDetails.Deductions.LateHours;
            totalOvertimeHours = salaryDetails.OvertimeHours;
            totalWorkedHours = salaryDetails.HoursWorked;

            var grossSalary = salaryDetails.GrossSalary;
            var netSalary = salaryDetails.NetSalary;
            var overtimePay = salaryDetails.OvertimePay;
            var userBonusAmount = salaryDetails.BonusAmount;
            var employeeInsuranceAmount = salaryDetails.Insurance.Social + salaryDetails.Insurance.Health;
            var companyInsuranceAmount = salaryDetails.Insurance.CompanyShare;
            var insuranceSalary = salaryDetails.Insurance.InsuranceSalary;
            var taxAmount = salaryDetails.TaxAmount;
            var penaltyDeductionAmount = salaryDetails.Deductions.PenaltiesAmount;

            // Build employee name
            string employeeName = string.Empty;
            if (!string.IsNullOrWhiteSpace(user.FirstNameAr) || !string.IsNullOrWhiteSpace(user.LastNameAr))
            {
                employeeName = $"{user.FirstNameAr} {user.MiddleNameAr} {user.LastNameAr}".Trim();
            }
            else if (!string.IsNullOrWhiteSpace(user.FirstNameEn) || !string.IsNullOrWhiteSpace(user.LastNameEn))
            {
                employeeName = $"{user.FirstNameEn} {user.MiddleNameEn} {user.LastNameEn}".Trim();
            }

            // Get department name
            string? departmentName = null;
            if (user.DepartmentId.HasValue && departments.TryGetValue(user.DepartmentId.Value, out var deptName))
            {
                departmentName = deptName;
            }

            string? jobTitleName = null;
            if (user.JobId.HasValue && jobTitlesMonthly.TryGetValue(user.JobId.Value, out var jtName))
            {
                jobTitleName = jtName;
            }

            results.Add(new MonthlyReportResponse
            {
                UserId = user.Id,
                Name = employeeName,
                EmployeeStatusNote = salaryDetails.EmployeeStatusNote,
                DepartmentName = departmentName,
                JobTitle = jobTitleName ?? user.JobTitle,
                TotalAttendance = totalAttendance,
                TotalAbsence = totalAbsence,
                AnnualLeave = Math.Round(annualLeave, 2),
                CasualLeave = Math.Round(casualLeave, 2),
                SickLeave = Math.Round(sickLeave, 2),
                TotalWorkedHoursInMonth = Math.Round(totalWorkedHours, 2),
                TotalHours = Math.Round(totalHours, 2),
                HoursDeducted = Math.Round(totalLateDeductionHours, 2),
                GrossSalary = Math.Round(grossSalary, 2),
                EmployeeInsuranceAmount = Math.Round(employeeInsuranceAmount, 2),
                CompanyInsuranceAmount = Math.Round(companyInsuranceAmount, 2),
                InsuranceSalary = Math.Round(insuranceSalary, 2),
                TaxAmount = Math.Round(taxAmount, 2),
                PenaltyDeductionAmount = Math.Round(penaltyDeductionAmount, 2),
                NetSalary = Math.Round(netSalary, 2),
                SalaryAfterDeduction = Math.Round(netSalary, 2),
                BonusAmount = Math.Round(userBonusAmount, 2),
                TotalOvertime = Math.Round(totalOvertimeHours, 2),
                OvertimeSalary = Math.Round(overtimePay, 2)
            });
        }

        return results
            .OrderBy(r => r.DepartmentName ?? string.Empty)
            .ThenBy(r => r.Name)
            .ToList();
    }

    public async Task<List<MonthlyReportDetailedResponse>> GetMonthlyReportDetailedForAllEmployeesAsync(int month, int year, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (month < 1 || month > 12)
            throw new InvalidOperationException("Invalid month. Month must be between 1 and 12.");
        if (year < 2000 || year > 2100)
            throw new InvalidOperationException("Invalid year.");

        var currentDate = DateTime.Now;
        var requestedDate = new DateOnly(year, month, 1);
        var currentMonthStart = new DateOnly(currentDate.Year, currentDate.Month, 1);
        if (requestedDate > currentMonthStart)
            throw new InvalidOperationException("لا يمكن طلب تقرير لشهر لم يأت بعد.");

        // Period: 26th previous month -> 25th current month
        var previousMonth = month == 1 ? 12 : month - 1;
        var previousYear = month == 1 ? year - 1 : year;
        var startDate = new DateOnly(previousYear, previousMonth, 26);
        var endDate = new DateOnly(year, month, 25);
        var effectiveEndDate = GetEffectivePeriodEnd(endDate);
        var startDateTime = startDate.ToDateTime(TimeOnly.MinValue);
        var endDateTime = endDate.ToDateTime(TimeOnly.MaxValue);

        var users = await _db.Users
            .AsNoTracking()
            .Include(u => u.WorkSchedule)
            .Where(u => u.IsActive)
            .ToListAsync(ct);

        var allAttendances = await _db.Attendances
            .AsNoTracking()
            .Where(a => a.Date >= startDate && a.Date <= endDate)
            .ToListAsync(ct);

        var allLeaves = await _db.Leaves
            .AsNoTracking()
            .Where(l => l.Status == RequestStatus.Approved &&
                       l.StartDate <= endDateTime &&
                       l.EndDate >= startDateTime)
            .ToListAsync(ct);

        var allPermissions = await _db.Permissions
            .AsNoTracking()
            .Where(p => p.Status == RequestStatus.Approved &&
                       p.Date >= startDateTime &&
                       p.Date <= endDateTime)
            .ToListAsync(ct);

        var allAssignments = await _db.Assignments
            .AsNoTracking()
            .Where(a => a.Status == RequestStatus.Approved &&
                       a.StartDate <= endDateTime &&
                       a.EndDate >= startDateTime)
            .ToListAsync(ct);

        var departmentIds = users.Where(u => u.DepartmentId.HasValue).Select(u => u.DepartmentId!.Value).Distinct().ToList();
        var departments = await _db.Departments
            .AsNoTracking()
            .Where(d => departmentIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Name, ct);

        var employmentModeIds = users.Where(u => u.EmploymentModeId.HasValue).Select(u => u.EmploymentModeId!.Value).Distinct().ToList();
        var employmentModes = await _db.EmploymentModes
            .AsNoTracking()
            .Where(m => employmentModeIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.Name, ct);

        var jobIdsDetailed = users.Where(u => u.JobId.HasValue).Select(u => u.JobId!.Value).Distinct().ToList();
        var jobTitlesDetailed = await _db.JobTitles
            .AsNoTracking()
            .Where(j => jobIdsDetailed.Contains(j.Id))
            .ToDictionaryAsync(j => j.Id, j => j.Name, ct);

        var results = new List<MonthlyReportDetailedResponse>();

        foreach (var user in users)
        {
            var userAttendances = allAttendances.Where(a => a.UserId == user.Id).ToList();
            var userLeaves = allLeaves.Where(l => l.UserId == user.Id).ToList();
            var userPermissions = allPermissions.Where(p => p.UserId == user.Id).ToList();
            var userAssignments = allAssignments.Where(a => a.UserId == user.Id).ToList();

            string? modeName = null;
            if (user.EmploymentModeId.HasValue && employmentModes.TryGetValue(user.EmploymentModeId.Value, out var mode))
                modeName = mode;

            var isFullTime = string.IsNullOrWhiteSpace(modeName) ||
                           string.Equals(modeName, "Full Time", StringComparison.OrdinalIgnoreCase);
            var isPartTime = string.Equals(modeName, "Part Time", StringComparison.OrdinalIgnoreCase);
            var isShift = string.Equals(modeName, "Shift", StringComparison.OrdinalIgnoreCase);
            var partTimeWorkDays = isPartTime ? ResolvePartTimeWorkDays(user.WorkSchedule) : null;

            var totalAttendance = userAttendances.Count(a => a.AttendanceTime.HasValue || a.DepartureTime.HasValue);

            var absenceDates = new List<DateOnly>();
            for (var date = startDate; date <= effectiveEndDate; date = date.AddDays(1))
            {
                var dayOfWeek = date.DayOfWeek;
                var hasAttendance = userAttendances.Any(a => a.Date == date);

                if (!hasAttendance)
                {
                    bool isWorkDay = false;

                    if (isFullTime)
                    {
                        isWorkDay = IsFullTimeWeekday(dayOfWeek);
                    }
                    else if (isPartTime)
                    {
                        isWorkDay = partTimeWorkDays != null && partTimeWorkDays.Contains(dayOfWeek);
                    }
                    else if (isShift)
                    {
                        isWorkDay = false;
                    }

                    if (isWorkDay)
                    {
                        var hasApprovedLeave = userLeaves.Any(l => date >= DateOnly.FromDateTime(l.StartDate) && date <= DateOnly.FromDateTime(l.EndDate));
                        var hasApprovedAssignment = userAssignments.Any(a => date >= DateOnly.FromDateTime(a.StartDate) && date <= DateOnly.FromDateTime(a.EndDate));
                        var isPublicHoliday = await IsPublicHolidayForUserAsync(date, user.Id, user.DepartmentId, user.EmploymentModeId, user.Religion, ct);

                        if (!hasApprovedLeave && !hasApprovedAssignment && !isPublicHoliday)
                            absenceDates.Add(date);
                    }
                }
            }
            var totalAbsence = absenceDates.Count;

            decimal annualLeave = 0m;
            decimal casualLeave = 0m;
            decimal sickLeave = 0m;
            foreach (var leave in userLeaves)
            {
                var leaveStart = DateOnly.FromDateTime(leave.StartDate);
                var leaveEnd = DateOnly.FromDateTime(leave.EndDate);
                var reportStart = leaveStart > startDate ? leaveStart : startDate;
                var reportEnd = leaveEnd < endDate ? leaveEnd : endDate;
                if (reportStart <= reportEnd)
                {
                    var days = CalculateWorkingDaysForLeave(reportStart, reportEnd);
                    switch (leave.LeaveType)
                    {
                        case LeaveType.Annual:
                            annualLeave += days;
                            break;
                        case LeaveType.Casual:
                            casualLeave += days;
                            break;
                        case LeaveType.Sick:
                            sickLeave += days;
                            break;
                    }
                }
            }

            decimal totalWorkedHours = 0m;
            decimal totalLateDeductionHours = 0m;
            decimal totalOvertimeHours = 0m;
            var lateDates = new List<DateOnly>();

            foreach (var attendance in userAttendances)
            {
                var scheduledTimes = await ResolveScheduledTimesByEmploymentModeAsync(user, attendance.Date, ct);
                var scheduledStart = scheduledTimes.Start;
                var scheduledEnd = scheduledTimes.End;

                decimal dayHours = 0m;
                if (attendance.AttendanceTime.HasValue && attendance.DepartureTime.HasValue)
                {
                    var hours = (attendance.DepartureTime.Value.ToTimeSpan() - attendance.AttendanceTime.Value.ToTimeSpan()).TotalHours;
                    if (hours > 0)
                        dayHours = (decimal)hours;
                }
                else if (attendance.AttendanceTime.HasValue && !attendance.DepartureTime.HasValue)
                {
                    var endTime = scheduledEnd ?? DefaultFullTimeEnd;
                    var hours = (endTime.ToTimeSpan() - attendance.AttendanceTime.Value.ToTimeSpan()).TotalHours;
                    if (hours > 0)
                        dayHours = (decimal)hours;
                }
                else if (!attendance.AttendanceTime.HasValue && attendance.DepartureTime.HasValue)
                {
                    var startTime = scheduledStart ?? DefaultFullTimeStart;
                    var hours = (attendance.DepartureTime.Value.ToTimeSpan() - startTime.ToTimeSpan()).TotalHours;
                    if (hours > 0)
                        dayHours = (decimal)hours;
                }

                totalWorkedHours += dayHours;

                var overtimeHours = isShift
                    ? CalculateShiftOvertimeHours(attendance)
                    : CalculateOvertime(attendance.DepartureTime, scheduledEnd);
                totalOvertimeHours += overtimeHours;

                var (_, _, lateHours) = CalculateLateDeduction(attendance.AttendanceTime, scheduledStart);
                if (lateHours > 0)
                {
                    var dayPermissions = userPermissions
                        .Where(p => p.Date.Date == attendance.Date.ToDateTime(TimeOnly.MinValue).Date)
                        .ToList();

                    var hasApprovedAssignment = userAssignments.Any(a =>
                        attendance.Date >= DateOnly.FromDateTime(a.StartDate) &&
                        attendance.Date <= DateOnly.FromDateTime(a.EndDate));

                    var hasApprovedLeave = userLeaves.Any(l =>
                        attendance.Date >= DateOnly.FromDateTime(l.StartDate) &&
                        attendance.Date <= DateOnly.FromDateTime(l.EndDate));

                    if (dayPermissions.Any() || hasApprovedAssignment || hasApprovedLeave)
                        lateHours = 0m;
                }

                if (lateHours > 0)
                    lateDates.Add(attendance.Date);
                totalLateDeductionHours += lateHours;
            }

            decimal totalHours = 0m;
            for (var date = startDate; date <= effectiveEndDate; date = date.AddDays(1))
            {
                var dayOfWeek = date.DayOfWeek;
                bool isWorkDay = false;
                TimeOnly? startTime = null;
                TimeOnly? endTime = null;

                if (isFullTime)
                {
                    if (IsFullTimeWeekday(dayOfWeek))
                    {
                        isWorkDay = true;
                        startTime = user.WorkSchedule?.FullTimeStartOverride ?? DefaultFullTimeStart;
                        endTime = user.WorkSchedule?.FullTimeEndOverride ?? DefaultFullTimeEnd;
                    }
                }
                else if (isPartTime)
                {
                    if (partTimeWorkDays != null &&
                        partTimeWorkDays.Contains(dayOfWeek) &&
                        user.WorkSchedule?.PartTimeStart.HasValue == true &&
                        user.WorkSchedule?.PartTimeEnd.HasValue == true)
                    {
                        isWorkDay = true;
                        startTime = user.WorkSchedule.PartTimeStart;
                        endTime = user.WorkSchedule.PartTimeEnd;
                    }
                }
                else if (isShift)
                {
                    isWorkDay = false;
                }

                if (isWorkDay && startTime.HasValue && endTime.HasValue)
                {
                    var hours = (endTime.Value.ToTimeSpan() - startTime.Value.ToTimeSpan()).TotalHours;
                    if (hours > 0)
                        totalHours += (decimal)hours;
                }
            }

            var salaryDetails = await CalculateSalaryAsync(new CalculateSalaryRequest
            {
                EmployeeId = user.Id,
                Month = month,
                Year = year
            }, ct);

            if (salaryDetails.EmployeeStatusNote != null)
            {
                totalAttendance = 0;
                totalAbsence = 0;
                absenceDates.Clear();
                annualLeave = 0m;
                casualLeave = 0m;
                sickLeave = 0m;
                totalWorkedHours = 0m;
                totalLateDeductionHours = 0m;
                totalOvertimeHours = 0m;
                totalHours = 0m;
                lateDates.Clear();
            }

            totalAbsence = salaryDetails.Deductions.AbsenceDays;
            totalLateDeductionHours = salaryDetails.Deductions.LateHours;
            totalOvertimeHours = salaryDetails.OvertimeHours;
            totalWorkedHours = salaryDetails.HoursWorked;
            var overtimePay = salaryDetails.OvertimePay;
            var userBonusAmount = salaryDetails.BonusAmount;
            var netSalary = salaryDetails.NetSalary;
            var grossSalary = salaryDetails.GrossSalary;
            var housingAllowance = salaryDetails.Allowances.Housing;
            var mealAllowance = salaryDetails.Allowances.Meal;
            var transportationAllowance = salaryDetails.Allowances.Transportation;
            var insuranceAllowance = salaryDetails.Allowances.Insurance;
            var insuranceSalary = salaryDetails.Insurance.InsuranceSalary;
            var lateDeductionAmount = salaryDetails.Deductions.LateAmount;
            var absenceDeductionAmount = salaryDetails.Deductions.AbsenceAmount;
            var reportWorkingDays = salaryDetails.TotalWorkingDays;
            var shiftRate = salaryDetails.ShiftRate ?? 0m;

            string employeeName = string.Empty;
            if (!string.IsNullOrWhiteSpace(user.FirstNameAr) || !string.IsNullOrWhiteSpace(user.LastNameAr))
                employeeName = $"{user.FirstNameAr} {user.MiddleNameAr} {user.LastNameAr}".Trim();
            else if (!string.IsNullOrWhiteSpace(user.FirstNameEn) || !string.IsNullOrWhiteSpace(user.LastNameEn))
                employeeName = $"{user.FirstNameEn} {user.MiddleNameEn} {user.LastNameEn}".Trim();

            string? departmentName = null;
            if (user.DepartmentId.HasValue && departments.TryGetValue(user.DepartmentId.Value, out var deptName))
                departmentName = deptName;

            string? jobTitleName = null;
            if (user.JobId.HasValue && jobTitlesDetailed.TryGetValue(user.JobId.Value, out var jtName))
                jobTitleName = jtName;

            results.Add(new MonthlyReportDetailedResponse
            {
                UserId = user.Id,
                Name = employeeName,
                EmployeeStatusNote = salaryDetails.EmployeeStatusNote,
                DepartmentName = departmentName,
                JobTitle = jobTitleName ?? user.JobTitle,
                EmploymentMode = modeName,
                GrossSalary = Math.Round(grossSalary, 2),
                NetSalary = Math.Round(netSalary, 2),
                ShiftRate = Math.Round(shiftRate, 2),
                HousingAllowance = Math.Round(housingAllowance, 2),
                MealAllowance = Math.Round(mealAllowance, 2),
                TransportationAllowance = Math.Round(transportationAllowance, 2),
                InsuranceAllowance = Math.Round(insuranceAllowance, 2),
                BonusAmount = Math.Round(userBonusAmount, 2),
                InsuranceSalary = Math.Round(insuranceSalary, 2),
                TotalWorkingDays = reportWorkingDays,
                TotalAttendance = totalAttendance,
                TotalAbsence = totalAbsence,
                AbsenceDates = absenceDates.Select(d => d.ToString("yyyy-MM-dd")).ToList(),
                AnnualLeave = Math.Round(annualLeave, 2),
                CasualLeave = Math.Round(casualLeave, 2),
                SickLeave = Math.Round(sickLeave, 2),
                TotalWorkedHoursInMonth = Math.Round(totalWorkedHours, 2),
                TotalHoursRequired = Math.Round(totalHours, 2),
                HoursDeducted = Math.Round(totalLateDeductionHours, 2),
                LateDeductionAmount = Math.Round(lateDeductionAmount, 2),
                AbsenceDeductionAmount = Math.Round(absenceDeductionAmount, 2),
                AdvanceDeductionAmount = Math.Round(salaryDetails.Deductions.AdvancesAmount, 2),
                HealthInsuranceDeductionAmount = Math.Round(salaryDetails.Deductions.HealthInsuranceAmount, 2),
                LateDates = lateDates.Select(d => d.ToString("yyyy-MM-dd")).ToList(),
                SalaryAfterDeduction = Math.Round(netSalary, 2),
                OvertimeSalary = Math.Round(overtimePay, 2)
            });
        }

        return results
            .OrderBy(r => r.DepartmentName ?? string.Empty)
            .ThenBy(r => r.Name)
            .ToList();
    }

    public async Task<List<ShiftMonthlyReportResponse>> GetShiftMonthlyReportAsync(int month, int year, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (month < 1 || month > 12)
            throw new InvalidOperationException("Invalid month. Month must be between 1 and 12.");
        if (year < 2000 || year > 2100)
            throw new InvalidOperationException("Invalid year.");

        // Validate that the requested month is not in the future
        var currentDate = DateTime.Now;
        var requestedDate = new DateOnly(year, month, 1);
        var currentMonthStart = new DateOnly(currentDate.Year, currentDate.Month, 1);
        if (requestedDate > currentMonthStart)
            throw new InvalidOperationException("لا يمكن طلب تقرير لشهر لم يأت بعد.");

        // Period: 26th previous month -> 25th current month
        var previousMonth = month == 1 ? 12 : month - 1;
        var previousYear = month == 1 ? year - 1 : year;
        var startDate = new DateOnly(previousYear, previousMonth, 26);
        var endDate = new DateOnly(year, month, 25);

        var users = await _db.Users
            .AsNoTracking()
            .Include(u => u.WorkSchedule)
            .Where(u => u.IsActive)
            .ToListAsync(ct);

        var employmentModeIds = users.Where(u => u.EmploymentModeId.HasValue).Select(u => u.EmploymentModeId!.Value).Distinct().ToList();
        var employmentModes = await _db.EmploymentModes
            .AsNoTracking()
            .Where(m => employmentModeIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.Name, ct);

        var shiftUsers = users
            .Where(u => u.EmploymentModeId.HasValue &&
                        employmentModes.TryGetValue(u.EmploymentModeId.Value, out var mode) &&
                        string.Equals(mode, "Shift", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var allAttendances = await _db.Attendances
            .AsNoTracking()
            .Where(a => a.Date >= startDate && a.Date <= endDate)
            .ToListAsync(ct);

        var departmentIds = shiftUsers.Where(u => u.DepartmentId.HasValue).Select(u => u.DepartmentId!.Value).Distinct().ToList();
        var departments = await _db.Departments
            .AsNoTracking()
            .Where(d => departmentIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Name, ct);

        var results = new List<ShiftMonthlyReportResponse>();

        foreach (var user in shiftUsers)
        {
            var userAttendances = allAttendances
                .Where(a => a.UserId == user.Id && a.AttendanceTime.HasValue)
                .OrderBy(a => a.Date)
                .ToList();

            var paidShiftDays = userAttendances
                .Select(a => a.Date)
                .Distinct()
                .Count();

            decimal totalWorkedHours = 0m;
            var shiftDates = new List<string>();

            foreach (var attendance in userAttendances)
            {
                var dayHours = CalculateShiftDayHours(attendance);
                totalWorkedHours += dayHours;

                var datePart = attendance.Date.ToString("dd/MM/yyyy");
                var inTime = attendance.AttendanceTime?.ToString("HH:mm") ?? "-";
                var outTime = attendance.DepartureTime?.ToString("HH:mm") ?? "-";
                shiftDates.Add($"{datePart} {inTime} - {outTime}");
            }

            var hourlyRate = (user.ShiftRate ?? 0m) / 8m;
            var salary = hourlyRate * totalWorkedHours;

            string employeeName = string.Empty;
            if (!string.IsNullOrWhiteSpace(user.FirstNameAr) || !string.IsNullOrWhiteSpace(user.LastNameAr))
                employeeName = $"{user.FirstNameAr} {user.MiddleNameAr} {user.LastNameAr}".Trim();
            else if (!string.IsNullOrWhiteSpace(user.FirstNameEn) || !string.IsNullOrWhiteSpace(user.LastNameEn))
                employeeName = $"{user.FirstNameEn} {user.MiddleNameEn} {user.LastNameEn}".Trim();

            string? departmentName = null;
            if (user.DepartmentId.HasValue && departments.TryGetValue(user.DepartmentId.Value, out var deptName))
                departmentName = deptName;

            results.Add(new ShiftMonthlyReportResponse
            {
                UserId = user.Id,
                Name = employeeName,
                DepartmentName = departmentName,
                JobTitle = user.JobTitle,
                ShiftRate = user.ShiftRate ?? 0m,
                PaidShiftDays = paidShiftDays,
                TotalWorkedHours = Math.Round(totalWorkedHours, 2),
                Salary = Math.Round(salary, 2),
                ShiftDates = shiftDates
            });
        }

        return results.OrderBy(r => r.Name).ToList();
    }

    public async Task<List<ShiftMonthlyReportItemResponse>> GetShiftMonthlyReportItemsAsync(int month, int year, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (month < 1 || month > 12)
            throw new InvalidOperationException("Invalid month. Month must be between 1 and 12.");
        if (year < 2000 || year > 2100)
            throw new InvalidOperationException("Invalid year.");

        var currentDate = DateTime.Now;
        var requestedDate = new DateOnly(year, month, 1);
        var currentMonthStart = new DateOnly(currentDate.Year, currentDate.Month, 1);
        if (requestedDate > currentMonthStart)
            throw new InvalidOperationException("لا يمكن طلب تقرير لشهر لم يأت بعد.");

        var previousMonth = month == 1 ? 12 : month - 1;
        var previousYear = month == 1 ? year - 1 : year;
        var startDate = new DateOnly(previousYear, previousMonth, 26);
        var endDate = new DateOnly(year, month, 25);

        var users = await _db.Users
            .AsNoTracking()
            .Include(u => u.WorkSchedule)
            .Where(u => u.IsActive)
            .ToListAsync(ct);

        var employmentModeIds = users.Where(u => u.EmploymentModeId.HasValue).Select(u => u.EmploymentModeId!.Value).Distinct().ToList();
        var employmentModes = await _db.EmploymentModes
            .AsNoTracking()
            .Where(m => employmentModeIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.Name, ct);

        var shiftUsers = users
            .Where(u => u.EmploymentModeId.HasValue &&
                        employmentModes.TryGetValue(u.EmploymentModeId.Value, out var mode) &&
                        string.Equals(mode, "Shift", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var allAttendances = await _db.Attendances
            .AsNoTracking()
            .Where(a => a.Date >= startDate && a.Date <= endDate && a.AttendanceTime.HasValue)
            .ToListAsync(ct);

        var departmentIds = shiftUsers.Where(u => u.DepartmentId.HasValue).Select(u => u.DepartmentId!.Value).Distinct().ToList();
        var departments = await _db.Departments
            .AsNoTracking()
            .Where(d => departmentIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Name, ct);

        var items = new List<ShiftMonthlyReportItemResponse>();

        foreach (var user in shiftUsers)
        {
            var userAttendances = allAttendances
                .Where(a => a.UserId == user.Id)
                .ToList();

            var groupedByDate = userAttendances
                .GroupBy(a => a.Date)
                .OrderBy(g => g.Key)
                .ToList();

            var paidShiftDays = groupedByDate.Count;
            decimal totalWorkedHours = 0m;

            // Precompute monthly totals
            foreach (var group in groupedByDate)
            {
                var attendance = group
                    .OrderBy(a => a.AttendanceTime ?? TimeOnly.MinValue)
                    .First();

                var departure = group
                    .OrderByDescending(a => a.DepartureTime ?? TimeOnly.MinValue)
                    .First();

                var dayHours = CalculateShiftDayHours(attendance.AttendanceTime, departure.DepartureTime);
                totalWorkedHours += dayHours;
            }

            var hourlyRate = (user.ShiftRate ?? 0m) / 8m;
            var monthlySalary = hourlyRate * totalWorkedHours;

            string employeeName = string.Empty;
            if (!string.IsNullOrWhiteSpace(user.FirstNameAr) || !string.IsNullOrWhiteSpace(user.LastNameAr))
                employeeName = $"{user.FirstNameAr} {user.MiddleNameAr} {user.LastNameAr}".Trim();
            else if (!string.IsNullOrWhiteSpace(user.FirstNameEn) || !string.IsNullOrWhiteSpace(user.LastNameEn))
                employeeName = $"{user.FirstNameEn} {user.MiddleNameEn} {user.LastNameEn}".Trim();

            string? departmentName = null;
            if (user.DepartmentId.HasValue && departments.TryGetValue(user.DepartmentId.Value, out var deptName))
                departmentName = deptName;

            foreach (var group in groupedByDate)
            {
                var attendance = group
                    .OrderBy(a => a.AttendanceTime ?? TimeOnly.MinValue)
                    .First();

                var departure = group
                    .OrderByDescending(a => a.DepartureTime ?? TimeOnly.MinValue)
                    .First();

                var dayHours = CalculateShiftDayHours(attendance.AttendanceTime, departure.DepartureTime);

                items.Add(new ShiftMonthlyReportItemResponse
                {
                    UserId = user.Id,
                    Name = employeeName,
                    DepartmentName = departmentName,
                    JobTitle = user.JobTitle,
                    ShiftRate = user.ShiftRate ?? 0m,
                    TotalShiftDays = paidShiftDays,
                    TotalWorkedHours = Math.Round(totalWorkedHours, 2),
                    MonthlySalary = Math.Round(monthlySalary, 2),
                    Date = group.Key,
                    AttendanceTime = attendance.AttendanceTime,
                    DepartureTime = departure.DepartureTime,
                    DayWorkedHours = Math.Round(dayHours, 2)
                });
            }
        }

        return items
            .OrderBy(i => i.Name)
            .ThenBy(i => i.Date)
            .ToList();
    }

    private decimal CalculateWorkingDaysForLeave(DateOnly startDate, DateOnly endDate)
    {
        var days = 0m;
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            // Exclude Friday (5) and Saturday (6)
            if (currentDate.DayOfWeek != DayOfWeek.Friday && currentDate.DayOfWeek != DayOfWeek.Saturday)
            {
                days++;
            }
            currentDate = currentDate.AddDays(1);
        }

        return days;
    }

    private async Task<bool> IsPublicHolidayForUserAsync(DateOnly date, Guid userId, int? departmentId, int? employmentModeId, Religion? religion, CancellationToken ct)
    {
        // Check if date is a public holiday
        var holiday = await _db.PublicHolidays
            .Include(h => h.Exceptions)
            .FirstOrDefaultAsync(h => h.Date == date && h.IsActive, ct);

        if (holiday == null)
            return false;

        // Check if there's an exception for this user
        var hasException = await _db.PublicHolidayExceptions
            .AnyAsync(e => e.PublicHolidayId == holiday.Id &&
                          (e.EmployeeId == userId ||
                           (e.DepartmentId.HasValue && e.DepartmentId == departmentId) ||
                           (e.EmploymentModeId.HasValue && e.EmploymentModeId == employmentModeId) ||
                           (e.Religion.HasValue && e.Religion == religion)), ct);

        // If there's an exception, it's NOT a holiday for this user
        // If there's no exception, it IS a holiday for this user
        return !hasException;
    }

    private async Task<(decimal employeeAmount, decimal companyAmount)> CalculateInsuranceAsync(decimal baseSalary, CancellationToken ct)
    {
        // Get active insurance settings
        var insuranceSettings = await _db.InsuranceSettings
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (insuranceSettings == null)
            return (0m, 0m);

        // Determine the base amount for calculation (apply min/max limits)
        decimal calculationBase;
        if (baseSalary < insuranceSettings.MinimumAmount)
        {
            calculationBase = insuranceSettings.MinimumAmount;
        }
        else if (baseSalary > insuranceSettings.MaximumAmount)
        {
            calculationBase = insuranceSettings.MaximumAmount;
        }
        else
        {
            calculationBase = baseSalary;
        }

        // Calculate employee and company insurance amounts
        var employeeAmount = calculationBase * insuranceSettings.EmployeePercentage / 100m;
        var companyAmount = calculationBase * insuranceSettings.CompanyPercentage / 100m;

        return (employeeAmount, companyAmount);
    }

    private async Task<decimal> CalculateSalaryAdvanceDeductionAsync(Guid userId, DateOnly periodEndDate, CancellationToken ct)
    {
        var advances = await _db.SalaryAdvances
            .Where(a => a.UserId == userId && a.Status == RequestStatus.Approved)
            .ToListAsync(ct);

        if (advances.Count == 0)
            return 0m;

        decimal total = 0m;

        foreach (var advance in advances)
        {
            var startDate = DateOnly.FromDateTime(advance.StartDate);
            if (startDate > periodEndDate)
                continue;

            var monthDiff = (periodEndDate.Year - startDate.Year) * 12 + (periodEndDate.Month - startDate.Month);
            if (monthDiff < 0 || monthDiff >= advance.NumberOfMonths)
                continue;

            var isLastInstallment = monthDiff == advance.NumberOfMonths - 1;
            var installmentAmount = isLastInstallment
                ? advance.Amount - (advance.MonthlyDeduction * (advance.NumberOfMonths - 1))
                : advance.MonthlyDeduction;

            if (installmentAmount < 0m)
                installmentAmount = 0m;

            total += installmentAmount;
        }

        return total;
    }

    private async Task<decimal> CalculateHealthInsuranceDeductionAsync(Guid userId, DateOnly periodStartDate, DateOnly periodEndDate, CancellationToken ct)
    {
        var enrollments = await _db.HealthInsuranceEnrollments
            .Where(e => e.UserId == userId && e.IsActive)
            .ToListAsync(ct);

        if (enrollments.Count == 0)
            return 0m;

        decimal total = 0m;
        foreach (var e in enrollments)
        {
            var start = DateOnly.FromDateTime(e.StartDate);
            var end = e.EndDate.HasValue ? DateOnly.FromDateTime(e.EndDate.Value) : (DateOnly?)null;

            // Deduct if coverage overlaps the payroll period (26 -> 25)
            var overlaps = end.HasValue
                ? start <= periodEndDate && end.Value >= periodStartDate
                : start <= periodEndDate;

            if (!overlaps)
                continue;

            total += e.MonthlyPremium;
        }

        return total;
    }

    private async Task<decimal> CalculateTaxAsync(decimal baseSalary, CancellationToken ct)
    {
        // Convert monthly salary to annual salary
        var annualSalary = baseSalary * 12m;
        
        // Get active tax brackets ordered by Order
        var taxBrackets = await _db.TaxBrackets
            .Where(b => b.IsActive)
            .OrderBy(b => b.Order)
            .ToListAsync(ct);

        if (!taxBrackets.Any())
            return 0m;

        decimal remainingSalary = annualSalary;

        // Step 1: Deduct all exempt brackets (0% tax) from the salary
        foreach (var bracket in taxBrackets)
        {
            if (bracket.Percentage == 0m)
            {
                if (bracket.ToAmount.HasValue)
                {
                    var bracketRange = bracket.ToAmount.Value - bracket.FromAmount;
                    
                    if (remainingSalary >= bracketRange)
                    {
                        remainingSalary -= bracketRange;
                    }
                    else
                    {
                        // Salary falls within this exempt bracket, no tax
                        return 0m;
                    }
                }
            }
        }

        // Step 2: Find which bracket the remaining salary falls into
        foreach (var bracket in taxBrackets)
        {
            // Skip exempt brackets (already deducted)
            if (bracket.Percentage == 0m)
                continue;

            if (bracket.ToAmount.HasValue)
            {
                // Check if remaining salary falls within this bracket's range
                if (remainingSalary >= bracket.FromAmount && remainingSalary <= bracket.ToAmount.Value)
                {
                    // Apply this bracket's tax rate to the entire remaining salary
                    var annualTax = remainingSalary * bracket.Percentage / 100m;
                    return annualTax / 12m;
                }
            }
            else
            {
                // Last bracket (no upper limit)
                if (remainingSalary >= bracket.FromAmount)
                {
                    // Apply this bracket's tax rate to the entire remaining salary
                    var annualTax = remainingSalary * bracket.Percentage / 100m;
                    return annualTax / 12m;
                }
            }
        }

        // If no bracket matches, return 0
        return 0m;
    }

    private async Task<(decimal totalAmount, List<PenaltyInfo> penalties)> CalculatePenaltiesAsync(
        Guid userId, 
        decimal baseSalary, 
        int month, 
        int year, 
        CancellationToken ct)
    {
        // Get all pending penalties for this employee
        var pendingPenalties = await _db.EmployeePenalties
            .Where(p => p.UserId == userId && !p.IsApplied)
            .OrderBy(p => p.PenaltyDate)
            .ToListAsync(ct);

        if (!pendingPenalties.Any())
            return (0m, new List<PenaltyInfo>());

        var dailySalary = baseSalary / 30m;
        decimal totalPenaltyAmount = 0m;
        var appliedPenalties = new List<PenaltyInfo>();

        foreach (var penalty in pendingPenalties)
        {
            decimal penaltyAmount = 0m;

            if (penalty.PenaltyType == PenaltyType.Days && penalty.Days.HasValue)
            {
                // Calculate penalty based on days
                penaltyAmount = penalty.Days.Value * dailySalary;
            }
            else if (penalty.PenaltyType == PenaltyType.FixedAmount && penalty.Amount.HasValue)
            {
                // Use fixed amount
                penaltyAmount = penalty.Amount.Value;
            }

            totalPenaltyAmount += penaltyAmount;

            // Mark penalty as applied
            penalty.IsApplied = true;
            penalty.AppliedMonth = month;
            penalty.AppliedYear = year;

            // Add to response list
            appliedPenalties.Add(new PenaltyInfo
            {
                Id = penalty.Id,
                PenaltyType = penalty.PenaltyType.ToString(),
                Days = penalty.Days,
                Amount = penalty.Amount,
                PenaltyDate = penalty.PenaltyDate,
                Reason = penalty.Reason
            });
        }

        // Save changes to mark penalties as applied
        await _db.SaveChangesAsync(ct);

        return (totalPenaltyAmount, appliedPenalties);
    }

    public async Task<PayslipResponse> GetPayslipAsync(Guid userId, int month, int year, CancellationToken ct)
    {
        // 1. Fetch User with related data for the header
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
            throw new InvalidOperationException("Employee not found.");

        string? departmentName = null;
        if (user.DepartmentId.HasValue)
        {
            var dept = await _db.Departments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == user.DepartmentId.Value, ct);
            departmentName = dept?.Name;
        }

        // 2. Calculate the core salary details using existing logic
        var salaryDetails = await CalculateSalaryAsync(new CalculateSalaryRequest
        {
            EmployeeId = userId,
            Month = month,
            Year = year
        }, ct);

        // 3. Determine actual working days
        int absenceDays = salaryDetails.Deductions.AbsenceDays;
        int actualWorkingDays = salaryDetails.TotalWorkingDays - absenceDays;
        if (actualWorkingDays < 0) actualWorkingDays = 0;

        // 4. Construct the wrapper
        // Set IssuedAt to the first day of the following month (when payslip is typically issued)
        var issuedDate = new DateTime(year, month, 1).AddMonths(1);
        
        return new PayslipResponse
        {
            EmployeeStatusNote = salaryDetails.EmployeeStatusNote,
            FullNameAr = $"{user.FirstNameAr} {user.MiddleNameAr} {user.LastNameAr}".Trim().Replace("  ", " "),
            FullNameEn = $"{user.FirstNameEn} {user.MiddleNameEn} {user.LastNameEn}".Trim().Replace("  ", " "),
            DepartmentName = departmentName,
            JobTitle = user.JobTitle,
            EmploymentMode = await _db.EmploymentModes
                .AsNoTracking()
                .Where(m => user.EmploymentModeId.HasValue && m.Id == user.EmploymentModeId.Value)
                .Select(m => m.Name)
                .FirstOrDefaultAsync(ct),
            Month = month,
            Year = year,
            ActualWorkingDays = actualWorkingDays,
            SalaryDetails = salaryDetails,
            IssuedAt = issuedDate
        };
    }
}
