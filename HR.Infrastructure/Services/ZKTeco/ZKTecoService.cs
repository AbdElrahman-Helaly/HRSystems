using System.Net.Http.Json;
using internalEmployee.Auth.Contracts;
using Microsoft.Extensions.Logging;
// using zkemkeeper; 

namespace internalEmployee.Services.ZKTeco;

public sealed class ZKTecoService : IZKTecoService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ZKTecoService>? _logger;
    private bool _isConnected = false;
    private string? _connectedIpAddress;
    
    // private CZKEMClass? _zkemkeeper;

    public ZKTecoService(HttpClient httpClient, ILogger<ZKTecoService>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public bool IsConnected => _isConnected;

    public Task<bool> ConnectAsync(string ipAddress, int port = 4370, int machineNumber = 1)
    {
        // Stub
        return Task.FromResult(false);
    }

    public Task DisconnectAsync()
    {
        // Stub
        return Task.CompletedTask;
    }

    public Task<List<ZKTecoDeviceLog>> GetLogsAsync(DateOnly? startDate = null, DateOnly? endDate = null)
    {
        // Stub
        return Task.FromResult(new List<ZKTecoDeviceLog>());
    }

    public Task<bool> SendAttendanceToApiAsync(ZKTecoDeviceLog log, string apiBaseUrl)
    {
         // Stub
         return Task.FromResult(false);
    }

    public Task<bool> SendDepartureToApiAsync(ZKTecoDeviceLog log, string apiBaseUrl)
    {
        // Stub
         return Task.FromResult(false);
    }

    public Task<int> ProcessAndSendLogsAsync(string apiBaseUrl, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        return Task.FromResult(0);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
