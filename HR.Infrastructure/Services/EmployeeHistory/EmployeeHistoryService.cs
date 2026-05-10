using internalEmployee.Data;
using internalEmployee.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace internalEmployee.Services.EmployeeHistory;

public sealed class EmployeeHistoryService : IEmployeeHistoryService
{
    private readonly AppDbContext _context;

    public EmployeeHistoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Data.Entities.EmployeeHistory> CreateHistoryAsync(
        CreateEmployeeHistoryRequest request, 
        Guid? doneByUserId, 
        CancellationToken ct = default)
    {
        var history = new Data.Entities.EmployeeHistory
        {
            EmployeeId = request.EmployeeId,
            EventType = request.EventType,
            OldValue = request.OldValue,
            NewValue = request.NewValue,
            Reason = request.Reason,
            Notes = request.Notes,
            OldSalary = request.OldSalary,
            NewSalary = request.NewSalary,
            OldDepartmentId = request.OldDepartmentId,
            NewDepartmentId = request.NewDepartmentId,
            OldBranchId = request.OldBranchId,
            NewBranchId = request.NewBranchId,
            OldJobId = request.OldJobId,
            NewJobId = request.NewJobId,
            OldManagerId = request.OldManagerId,
            NewManagerId = request.NewManagerId,
            EffectiveDate = request.EffectiveDate,
            EndDate = request.EndDate,
            DoneBy = doneByUserId,
            ApprovedBy = request.ApprovedBy,
            ApprovedDate = request.ApprovedBy.HasValue ? DateTime.Now : null,
            EventDate = DateTime.Now
        };

        _context.EmployeeHistories.Add(history);
        await _context.SaveChangesAsync(ct);

        return history;
    }

    public async Task<EmployeeHistoryListResponse> GetEmployeeHistoryAsync(
        GetEmployeeHistoryRequest request, 
        CancellationToken ct = default)
    {
        var query = _context.EmployeeHistories.AsQueryable();

        if (request.EmployeeId.HasValue)
            query = query.Where(h => h.EmployeeId == request.EmployeeId.Value);

        if (request.EventType.HasValue)
            query = query.Where(h => h.EventType == request.EventType.Value);

        if (request.StartDate.HasValue)
            query = query.Where(h => h.EventDate >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(h => h.EventDate <= request.EndDate.Value);

        var totalCount = await query.CountAsync(ct);

        var rawItems = await query
            .OrderByDescending(h => h.EventDate)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var items = await EnrichAndMapAsync(rawItems, ct);

        return new EmployeeHistoryListResponse
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public async Task<EmployeeHistorySummaryResponse> GetEmployeeHistorySummaryAsync(
        Guid employeeId, 
        CancellationToken ct = default)
    {
        var histories = await _context.EmployeeHistories
            .Where(h => h.EmployeeId == employeeId)
            .ToListAsync(ct);

        var employee = await _context.Users
            .Where(u => u.Id == employeeId)
            .Select(u => new { u.FirstNameAr, u.MiddleNameAr, u.LastNameAr })
            .FirstOrDefaultAsync(ct);

        var employeeName = employee != null 
            ? $"{employee.FirstNameAr} {employee.MiddleNameAr} {employee.LastNameAr}".Trim()
            : null;

        return new EmployeeHistorySummaryResponse
        {
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            TotalEvents = histories.Count,
            JobChanges = histories.Count(h => h.EventType == EmployeeEventType.JobTitleChange),
            SalaryChanges = histories.Count(h => h.EventType == EmployeeEventType.SalaryChange),
            Transfers = histories.Count(h => h.EventType == EmployeeEventType.Transfer),
            Promotions = histories.Count(h => h.EventType == EmployeeEventType.Promotion),
            ContractEvents = histories.Count(h => 
                h.EventType == EmployeeEventType.ContractCreated ||
                h.EventType == EmployeeEventType.ContractRenewed ||
                h.EventType == EmployeeEventType.ContractTerminated ||
                h.EventType == EmployeeEventType.ContractExpired),
            DisciplinaryActions = histories.Count(h => 
                h.EventType == EmployeeEventType.Warning ||
                h.EventType == EmployeeEventType.Investigation ||
                h.EventType == EmployeeEventType.Penalty ||
                h.EventType == EmployeeEventType.Suspension),
            LastEventDate = histories.OrderByDescending(h => h.EventDate).FirstOrDefault()?.EventDate
        };
    }

    public async Task<EmployeeHistoryResponse?> GetHistoryByIdAsync(
        int historyId, 
        CancellationToken ct = default)
    {
        var history = await _context.EmployeeHistories
            .Where(h => h.Id == historyId)
            .FirstOrDefaultAsync(ct);

        if (history == null) return null;

        var mapped = await EnrichAndMapAsync(new List<Data.Entities.EmployeeHistory> { history }, ct);
        return mapped.FirstOrDefault();
    }

    public async Task TrackJobChangeAsync(Guid employeeId, int? oldJobId, int? newJobId, string? reason, Guid? doneBy, CancellationToken ct = default)
    {
        if (oldJobId == newJobId) return;
        await CreateHistoryAsync(new CreateEmployeeHistoryRequest { 
            EmployeeId = employeeId, EventType = EmployeeEventType.JobTitleChange, 
            OldJobId = oldJobId, NewJobId = newJobId, Reason = reason 
        }, doneBy, ct);
    }

    public async Task TrackSalaryChangeAsync(Guid employeeId, decimal? oldSalary, decimal? newSalary, string? reason, Guid? doneBy, CancellationToken ct = default)
    {
        if (oldSalary == newSalary) return;
        await CreateHistoryAsync(new CreateEmployeeHistoryRequest { 
            EmployeeId = employeeId, EventType = EmployeeEventType.SalaryChange, 
            OldSalary = oldSalary, NewSalary = newSalary, Reason = reason 
        }, doneBy, ct);
    }

    public async Task TrackDepartmentChangeAsync(Guid employeeId, int? oldDeptId, int? newDeptId, string? reason, Guid? doneBy, CancellationToken ct = default)
    {
        if (oldDeptId == newDeptId) return;
        await CreateHistoryAsync(new CreateEmployeeHistoryRequest { 
            EmployeeId = employeeId, EventType = EmployeeEventType.DepartmentChange, 
            OldDepartmentId = oldDeptId, NewDepartmentId = newDeptId, Reason = reason 
        }, doneBy, ct);
    }

    public async Task TrackBranchChangeAsync(Guid employeeId, int? oldBranchId, int? newBranchId, string? reason, Guid? doneBy, CancellationToken ct = default)
    {
        if (oldBranchId == newBranchId) return;
        await CreateHistoryAsync(new CreateEmployeeHistoryRequest { 
            EmployeeId = employeeId, EventType = EmployeeEventType.BranchChange, 
            OldBranchId = oldBranchId, NewBranchId = newBranchId, Reason = reason 
        }, doneBy, ct);
    }

    public async Task TrackManagerChangeAsync(Guid employeeId, Guid? oldManagerId, Guid? newManagerId, string? reason, Guid? doneBy, CancellationToken ct = default)
    {
        if (oldManagerId == newManagerId) return;
        await CreateHistoryAsync(new CreateEmployeeHistoryRequest { 
            EmployeeId = employeeId, EventType = EmployeeEventType.ManagerChange, 
            OldManagerId = oldManagerId, NewManagerId = newManagerId, Reason = reason 
        }, doneBy, ct);
    }


    public async Task TrackPromotionAsync(Guid employeeId, int? oldJobId, int? newJobId, decimal? oldSalary, decimal? newSalary, string? reason, Guid? approvedBy, CancellationToken ct = default)
    {
        await CreateHistoryAsync(new CreateEmployeeHistoryRequest { 
            EmployeeId = employeeId, EventType = EmployeeEventType.Promotion, 
            OldJobId = oldJobId, NewJobId = newJobId, OldSalary = oldSalary, NewSalary = newSalary, 
            Reason = reason, ApprovedBy = approvedBy 
        }, approvedBy, ct);
    }

    private async Task<List<EmployeeHistoryResponse>> EnrichAndMapAsync(
        List<Data.Entities.EmployeeHistory> rawItems, 
        CancellationToken ct)
    {
        if (!rawItems.Any()) return new List<EmployeeHistoryResponse>();

        // Collect all IDs for bulk fetching
        var userIds = rawItems.SelectMany(r => new[] { r.EmployeeId, r.DoneBy, r.ApprovedBy, r.OldManagerId, r.NewManagerId })
            .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var deptIds = rawItems.SelectMany(r => new[] { r.OldDepartmentId, r.NewDepartmentId }).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var branchIds = rawItems.SelectMany(r => new[] { r.OldBranchId, r.NewBranchId }).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var jobIds = rawItems.SelectMany(r => new[] { r.OldJobId, r.NewJobId }).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();

        // Bulk Fetch Names
        var userNames = await _context.Users.Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => (u.FirstNameAr + " " + u.LastNameAr).Trim(), ct);
        var deptNames = await _context.Departments.Where(d => deptIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Name, ct);
        var branchNames = await _context.Branches.Where(b => branchIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.NameAr, ct);
        var jobNames = await _context.JobTitles.Where(j => jobIds.Contains(j.Id)).ToDictionaryAsync(j => j.Id, j => j.NameAr, ct);

        return rawItems.Select(h => {
            var response = new EmployeeHistoryResponse {
                Id = h.Id,
                EmployeeName = userNames.GetValueOrDefault(h.EmployeeId) ?? "Unknown",
                EventType = h.EventType.ToString(),
                Date = h.EventDate,
                DoneBy = h.DoneBy.HasValue ? userNames.GetValueOrDefault(h.DoneBy.Value) : null,
                Reason = h.Reason,
                Notes = h.Notes
            };

            // Aggregate all changes into the list
            if (h.OldSalary.HasValue || h.NewSalary.HasValue) {
                response.Changes.Add(new ChangeItem { 
                    Property = "الراتب", 
                    From = h.OldSalary?.ToString("N2"), 
                    To = h.NewSalary?.ToString("N2") 
                });
            }

            if (h.OldJobId.HasValue || h.NewJobId.HasValue) {
                response.Changes.Add(new ChangeItem { 
                    Property = "المسمى الوظيفي", 
                    From = h.OldJobId.HasValue ? jobNames.GetValueOrDefault(h.OldJobId.Value) : null, 
                    To = h.NewJobId.HasValue ? jobNames.GetValueOrDefault(h.NewJobId.Value) : null 
                });
            }

            if (h.OldDepartmentId.HasValue || h.NewDepartmentId.HasValue) {
                response.Changes.Add(new ChangeItem { 
                    Property = "القسم", 
                    From = h.OldDepartmentId.HasValue ? deptNames.GetValueOrDefault(h.OldDepartmentId.Value) : null, 
                    To = h.NewDepartmentId.HasValue ? deptNames.GetValueOrDefault(h.NewDepartmentId.Value) : null 
                });
            }

            if (h.OldBranchId.HasValue || h.NewBranchId.HasValue) {
                response.Changes.Add(new ChangeItem { 
                    Property = "الفرع", 
                    From = h.OldBranchId.HasValue ? branchNames.GetValueOrDefault(h.OldBranchId.Value) : null, 
                    To = h.NewBranchId.HasValue ? branchNames.GetValueOrDefault(h.NewBranchId.Value) : null 
                });
            }

            if (h.OldManagerId.HasValue || h.NewManagerId.HasValue) {
                response.Changes.Add(new ChangeItem { 
                    Property = "المدير المباشر", 
                    From = h.OldManagerId.HasValue ? userNames.GetValueOrDefault(h.OldManagerId.Value) : null, 
                    To = h.NewManagerId.HasValue ? userNames.GetValueOrDefault(h.NewManagerId.Value) : null 
                });
            }

            if (!string.IsNullOrEmpty(h.OldValue) || !string.IsNullOrEmpty(h.NewValue)) {
                response.Changes.Add(new ChangeItem { 
                    Property = "بيانات أخرى", 
                    From = h.OldValue, 
                    To = h.NewValue 
                });
            }

            // Build collective description
            var changeProps = response.Changes.Select(c => c.Property);
            response.Description = $"تحديث ({string.Join(" و ", changeProps)})";

            return response;
        }).ToList();
    }
}

