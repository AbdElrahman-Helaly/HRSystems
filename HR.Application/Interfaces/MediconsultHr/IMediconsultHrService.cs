using System.Net;

namespace internalEmployee.Services.MediconsultHr;

public interface IMediconsultHrService
{
    Task<(HttpStatusCode StatusCode, string Content)> GetApprovalsKpisAsync(
        string lang,
        string? fingerPrintId,
        int? mode,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        int? pageNumber,
        int? pageSize,
        CancellationToken ct);
}
