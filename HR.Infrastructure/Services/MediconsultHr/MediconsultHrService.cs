using System.Net;
using Microsoft.AspNetCore.WebUtilities;

namespace internalEmployee.Services.MediconsultHr;

public sealed class MediconsultHrService : IMediconsultHrService
{
    private const string BaseApiUrl = "https://api.medicardeg.com/api";
    private readonly HttpClient _httpClient;

    public MediconsultHrService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(HttpStatusCode StatusCode, string Content)> GetApprovalsKpisAsync(
        string lang,
        string? fingerPrintId,
        int? mode,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        int? pageNumber,
        int? pageSize,
        CancellationToken ct)
    {
        var url = BuildUrl(lang, fingerPrintId, mode, dateFrom, dateTo, pageNumber, pageSize);
        using var response = await _httpClient.GetAsync(url, ct);
        var content = await response.Content.ReadAsStringAsync(ct);
        return (response.StatusCode, content);
    }

    private static string BuildUrl(
        string lang,
        string? fingerPrintId,
        int? mode,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        int? pageNumber,
        int? pageSize)
    {
        var path = $"{BaseApiUrl}/{Uri.EscapeDataString(lang)}/MediconsultHR/GetApprovalsKPIs";

        var query = new Dictionary<string, string?>
        {
            ["mode"] = mode?.ToString(),
            ["dateFrom"] = dateFrom?.ToString("yyyy-MM-dd"),
            ["dateTo"] = dateTo?.ToString("yyyy-MM-dd"),
            ["pageNumber"] = pageNumber?.ToString(),
            ["pageSize"] = pageSize?.ToString()
        };

        if (!string.IsNullOrWhiteSpace(fingerPrintId))
        {
            query["FingerPrintId"] = fingerPrintId;
        }

        return QueryHelpers.AddQueryString(path, query);
    }
}
