using internalEmployee.Auth.Contracts;

namespace internalEmployee.Services.ZKTeco;

/// <summary>
/// Service interface for ZKTeco fingerprint device integration
/// </summary>
public interface IZKTecoService
{
    /// <summary>
    /// Connects to the ZKTeco device
    /// </summary>
    /// <param name="ipAddress">Device IP address</param>
    /// <param name="port">Device port (default: 4370)</param>
    /// <param name="machineNumber">Machine number (default: 1)</param>
    /// <returns>True if connection successful, false otherwise</returns>
    Task<bool> ConnectAsync(string ipAddress, int port = 4370, int machineNumber = 1);

    /// <summary>
    /// Disconnects from the ZKTeco device
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Gets attendance logs from the device
    /// </summary>
    /// <param name="startDate">Start date for filtering logs (optional)</param>
    /// <param name="endDate">End date for filtering logs (optional)</param>
    /// <returns>List of attendance logs</returns>
    Task<List<ZKTecoDeviceLog>> GetLogsAsync(DateOnly? startDate = null, DateOnly? endDate = null);

    /// <summary>
    /// Sends attendance record to the API
    /// </summary>
    /// <param name="log">Attendance log from device</param>
    /// <param name="apiBaseUrl">Base URL of the API</param>
    /// <returns>True if sent successfully, false otherwise</returns>
    Task<bool> SendAttendanceToApiAsync(ZKTecoDeviceLog log, string apiBaseUrl);

    /// <summary>
    /// Sends departure record to the API
    /// </summary>
    /// <param name="log">Departure log from device</param>
    /// <param name="apiBaseUrl">Base URL of the API</param>
    /// <returns>True if sent successfully, false otherwise</returns>
    Task<bool> SendDepartureToApiAsync(ZKTecoDeviceLog log, string apiBaseUrl);

    /// <summary>
    /// Processes all logs and sends them to the API
    /// </summary>
    /// <param name="apiBaseUrl">Base URL of the API</param>
    /// <param name="startDate">Start date for filtering logs (optional)</param>
    /// <param name="endDate">End date for filtering logs (optional)</param>
    /// <returns>Number of successfully processed logs</returns>
    Task<int> ProcessAndSendLogsAsync(string apiBaseUrl, DateOnly? startDate = null, DateOnly? endDate = null);

    /// <summary>
    /// Checks if device is connected
    /// </summary>
    bool IsConnected { get; }
}

