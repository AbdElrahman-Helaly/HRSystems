# ZKTeco Fingerprint Device Integration

This service provides integration with ZKTeco fingerprint devices for attendance tracking.

## Files

- `IZKTecoService.cs` - Interface for ZKTeco service
- `ZKTecoService.cs` - Implementation of ZKTeco service
- `ZKTecoDeviceLog.cs` - DTO for device log entries
- `ZKTecoWindowsFormsExample.cs` - Example usage in Windows Forms application

## Setup

### 1. Install ZKTeco SDK

To use this service with a real ZKTeco device, you need to:

1. Download the ZKTeco SDK (zkemkeeper.dll) from ZKTeco website
2. Add reference to `zkemkeeper.dll` in your project
3. Uncomment and implement the SDK calls in `ZKTecoService.cs`

### 2. Register Service (if using in ASP.NET Core)

Add to `Program.cs`:

```csharp
builder.Services.AddHttpClient<IZKTecoService, ZKTecoService>();
```

### 3. Configuration

Set the API base URL and device IP address in your application configuration or code.

## Usage Example

### In Windows Forms Application

```csharp
var httpClient = new HttpClient();
var zkTecoService = new ZKTecoService(httpClient);

// Connect to device
bool connected = await zkTecoService.ConnectAsync("192.168.1.201", 4370);

if (connected)
{
    // Get logs
    var logs = await zkTecoService.GetLogsAsync();
    
    // Send to API
    foreach (var log in logs)
    {
        if (log.IsCheckIn)
            await zkTecoService.SendAttendanceToApiAsync(log, "https://your-api.com");
        else
            await zkTecoService.SendDepartureToApiAsync(log, "https://your-api.com");
    }
    
    // Or process all at once
    await zkTecoService.ProcessAndSendLogsAsync("https://your-api.com");
    
    // Disconnect
    await zkTecoService.DisconnectAsync();
}
```

## API Endpoints

The service sends data to these endpoints:

- **Check-in**: `POST /api/Attendance/device/checkin`
- **Check-out**: `POST /api/Attendance/device/checkout`

Both endpoints accept:
```json
{
  "machineCode": "12345",
  "date": "2026-01-26",
  "time": "09:00:00"
}
```

## ZKTeco SDK Integration

To integrate with the actual ZKTeco SDK, you need to:

1. Add `zkemkeeper.dll` reference to your project
2. Uncomment the SDK code in `ZKTecoService.cs`
3. Replace placeholder implementations with actual SDK calls

Example SDK usage:
```csharp
using zkemkeeper;

var zkemkeeper = new CZKEMClass();
bool connected = zkemkeeper.Connect_Net("192.168.1.201", 4370);

if (connected)
{
    // Read logs
    zkemkeeper.ReadGeneralLogData(1);
    
    int dwEnrollNumber, dwVerifyMode, dwInOutMode;
    int dwYear, dwMonth, dwDay, dwHour, dwMinute, dwSecond, dwWorkCode;
    
    while (zkemkeeper.SSR_GetGeneralLogData(1, 
        out dwEnrollNumber, out dwVerifyMode, out dwInOutMode,
        out dwYear, out dwMonth, out dwDay, out dwHour, out dwMinute, out dwSecond, out dwWorkCode))
    {
        // Process log entry
    }
    
    zkemkeeper.Disconnect();
}
```

## Notes

- The current implementation includes placeholder code for testing
- Replace placeholder implementations with actual ZKTeco SDK calls for production use
- The service automatically handles date/time conversion and API communication
- All API calls are made without authentication (using MachineCode for identification)

