using internalEmployee.Auth.Contracts;
using internalEmployee.Auth.Models;
using internalEmployee.Data;
using internalEmployee.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace internalEmployee.Services.Custody;

public sealed class CustodyService : ICustodyService
{
    private readonly AppDbContext _db;

    public CustodyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CustodyItemResponse> CreateCustodyItemAsync(CreateCustodyItemRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var name = NormalizeRequiredText(request.Name, "Custody name is required.");

        var exists = await _db.CustodyItems
            .AnyAsync(x => x.Name.ToLower() == name.ToLower(), ct);

        if (exists)
            throw new InvalidOperationException("Custody item already exists.");

        var entity = new CustodyItem
        {
            Name = name,
            IsActive = request.IsActive,
            CreatedAt = DateTime.Now
        };

        _db.CustodyItems.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Map(entity);
    }

    public async Task<CustodyItemResponse> UpdateCustodyItemAsync(int id, UpdateCustodyItemRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var entity = await _db.CustodyItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity == null)
            throw new KeyNotFoundException("Custody item not found.");

        var name = NormalizeRequiredText(request.Name, "Custody name is required.");

        var exists = await _db.CustodyItems
            .AnyAsync(x => x.Id != id && x.Name.ToLower() == name.ToLower(), ct);

        if (exists)
            throw new InvalidOperationException("Custody item already exists.");

        entity.Name = name;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<bool> DeleteCustodyItemAsync(int id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var entity = await _db.CustodyItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity == null)
            return false;

        var isAssigned = await _db.EmployeeCustodies.AnyAsync(x => x.CustodyItemId == id, ct);
        if (isAssigned)
            throw new InvalidOperationException("Cannot delete custody item because it is already assigned to an employee.");

        _db.CustodyItems.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<CustodyItemResponse>> GetCustodyItemsAsync(bool activeOnly, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var query = _db.CustodyItems.AsNoTracking().AsQueryable();
        if (activeOnly)
            query = query.Where(x => x.IsActive);

        var items = await query
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

        return items.Select(Map).ToList();
    }

    public async Task<CustodyItemResponse?> GetCustodyItemByIdAsync(int id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var entity = await _db.CustodyItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return entity == null ? null : Map(entity);
    }

    public async Task<EmployeeCustodyResponse> CreateEmployeeCustodyAsync(CreateEmployeeCustodyRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await ValidateEmployeeCustodyRequestAsync(request.UserId, request.CustodyItemId, null, ct);

        var entity = new EmployeeCustody
        {
            UserId = request.UserId,
            CustodyItemId = request.CustodyItemId,
            Description = NormalizeOptionalText(request.Description),
            CreatedAt = DateTime.Now
        };

        _db.EmployeeCustodies.Add(entity);
        await _db.SaveChangesAsync(ct);

        return await GetRequiredEmployeeCustodyResponseAsync(entity.Id, ct);
    }

    public async Task<EmployeeCustodyResponse> UpdateEmployeeCustodyAsync(int id, UpdateEmployeeCustodyRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var entity = await _db.EmployeeCustodies.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity == null)
            throw new KeyNotFoundException("Employee custody record not found.");

        await ValidateEmployeeCustodyRequestAsync(request.UserId, request.CustodyItemId, id, ct);

        entity.UserId = request.UserId;
        entity.CustodyItemId = request.CustodyItemId;
        entity.Description = NormalizeOptionalText(request.Description);
        entity.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync(ct);
        return await GetRequiredEmployeeCustodyResponseAsync(id, ct);
    }

    public async Task<bool> DeleteEmployeeCustodyAsync(int id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var entity = await _db.EmployeeCustodies.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity == null)
            return false;

        _db.EmployeeCustodies.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<PaginatedResponse<EmployeeCustodyResponse>> GetEmployeeCustodiesAsync(
        Guid? userId,
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (pageNumber <= 0)
            pageNumber = 1;

        if (pageSize <= 0)
            pageSize = 10;

        if (pageSize > 100)
            pageSize = 100;

        var query =
            from employeeCustody in _db.EmployeeCustodies.AsNoTracking()
            join user in _db.Users.AsNoTracking() on employeeCustody.UserId equals user.Id
            join custodyItem in _db.CustodyItems.AsNoTracking() on employeeCustody.CustodyItemId equals custodyItem.Id
            join department in _db.Departments.AsNoTracking() on user.DepartmentId equals department.Id into departments
            from department in departments.DefaultIfEmpty()
            select new
            {
                employeeCustody.Id,
                employeeCustody.UserId,
                user.EmployeeCode,
                user.FirstNameAr,
                user.MiddleNameAr,
                user.LastNameAr,
                user.FirstNameEn,
                user.MiddleNameEn,
                user.LastNameEn,
                DepartmentName = department != null ? department.Name : null,
                user.JobTitle,
                employeeCustody.CustodyItemId,
                CustodyItemName = custodyItem.Name,
                employeeCustody.Description,
                employeeCustody.CreatedAt,
                employeeCustody.UpdatedAt
            };

        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);

        var normalizedSearch = NormalizeOptionalText(search);
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var pattern = $"%{normalizedSearch}%";
            query = query.Where(x =>
                (x.EmployeeCode != null && EF.Functions.Like(x.EmployeeCode, pattern)) ||
                (x.FirstNameAr != null && EF.Functions.Like(x.FirstNameAr, pattern)) ||
                (x.MiddleNameAr != null && EF.Functions.Like(x.MiddleNameAr, pattern)) ||
                (x.LastNameAr != null && EF.Functions.Like(x.LastNameAr, pattern)) ||
                (x.FirstNameEn != null && EF.Functions.Like(x.FirstNameEn, pattern)) ||
                (x.MiddleNameEn != null && EF.Functions.Like(x.MiddleNameEn, pattern)) ||
                (x.LastNameEn != null && EF.Functions.Like(x.LastNameEn, pattern)) ||
                EF.Functions.Like(x.CustodyItemName, pattern) ||
                (x.Description != null && EF.Functions.Like(x.Description, pattern)) ||
                (x.DepartmentName != null && EF.Functions.Like(x.DepartmentName, pattern)) ||
                (x.JobTitle != null && EF.Functions.Like(x.JobTitle, pattern)));
        }

        var totalCount = await query.CountAsync(ct);

        var entities = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResponse<EmployeeCustodyResponse>
        {
            Items = entities.Select(entity => new EmployeeCustodyResponse
            {
                Id = entity.Id,
                UserId = entity.UserId,
                EmployeeCode = entity.EmployeeCode,
                UserFullNameAr = BuildFullName(entity.FirstNameAr, entity.MiddleNameAr, entity.LastNameAr),
                UserFullNameEn = BuildFullName(entity.FirstNameEn, entity.MiddleNameEn, entity.LastNameEn),
                DepartmentName = entity.DepartmentName,
                JobTitle = entity.JobTitle,
                CustodyItemId = entity.CustodyItemId,
                CustodyItemName = entity.CustodyItemName,
                Description = entity.Description,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            }).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<EmployeeCustodyResponse?> GetEmployeeCustodyByIdAsync(int id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var entity = await _db.EmployeeCustodies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity == null)
            return null;

        var responses = await MapEmployeeCustodiesAsync(new List<EmployeeCustody> { entity }, ct);
        return responses[0];
    }

    public async Task<EmployeeCustodyLookupsResponse> GetEmployeeCustodyLookupsAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var users = await _db.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.FirstNameEn ?? u.FirstNameAr ?? u.EmployeeCode ?? u.PhoneNumber)
            .Select(u => new
            {
                u.Id,
                u.EmployeeCode,
                u.FirstNameAr,
                u.MiddleNameAr,
                u.LastNameAr,
                u.FirstNameEn,
                u.MiddleNameEn,
                u.LastNameEn,
                u.DepartmentId,
                u.JobTitle
            })
            .ToListAsync(ct);

        var departmentIds = users
            .Where(x => x.DepartmentId.HasValue)
            .Select(x => x.DepartmentId!.Value)
            .Distinct()
            .ToList();

        var departments = await _db.Departments
            .AsNoTracking()
            .Where(d => departmentIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Name, ct);

        var custodyItems = await _db.CustodyItems
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new EmployeeCustodyItemLookupItem
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToListAsync(ct);

        return new EmployeeCustodyLookupsResponse
        {
            Users = users.Select(u => new EmployeeCustodyUserLookupItem
            {
                UserId = u.Id,
                EmployeeCode = u.EmployeeCode,
                FullNameAr = BuildFullName(u.FirstNameAr, u.MiddleNameAr, u.LastNameAr),
                FullNameEn = BuildFullName(u.FirstNameEn, u.MiddleNameEn, u.LastNameEn),
                DepartmentName = u.DepartmentId.HasValue && departments.TryGetValue(u.DepartmentId.Value, out var departmentName)
                    ? departmentName
                    : null,
                JobTitle = u.JobTitle
            }).ToList(),
            CustodyItems = custodyItems
        };
    }

    private async Task ValidateEmployeeCustodyRequestAsync(Guid userId, int custodyItemId, int? currentId, CancellationToken ct)
    {
        var userExists = await _db.Users.AnyAsync(x => x.Id == userId, ct);
        if (!userExists)
            throw new InvalidOperationException("User not found.");

        var custodyItem = await _db.CustodyItems.FirstOrDefaultAsync(x => x.Id == custodyItemId, ct);
        if (custodyItem == null)
            throw new InvalidOperationException("Custody item not found.");

        if (!custodyItem.IsActive)
            throw new InvalidOperationException("Selected custody item is inactive.");

        var duplicateExists = await _db.EmployeeCustodies.AnyAsync(
            x => x.UserId == userId
                && x.CustodyItemId == custodyItemId
                && (!currentId.HasValue || x.Id != currentId.Value),
            ct);

        if (duplicateExists)
            throw new InvalidOperationException("This custody item is already assigned to the selected employee.");
    }

    private async Task<EmployeeCustodyResponse> GetRequiredEmployeeCustodyResponseAsync(int id, CancellationToken ct)
    {
        var response = await GetEmployeeCustodyByIdAsync(id, ct);
        if (response == null)
            throw new InvalidOperationException("Employee custody record not found.");

        return response;
    }

    private async Task<List<EmployeeCustodyResponse>> MapEmployeeCustodiesAsync(IReadOnlyCollection<EmployeeCustody> entities, CancellationToken ct)
    {
        if (entities.Count == 0)
            return new List<EmployeeCustodyResponse>();

        var userIds = entities.Select(x => x.UserId).Distinct().ToList();
        var custodyItemIds = entities.Select(x => x.CustodyItemId).Distinct().ToList();

        var users = await _db.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.EmployeeCode,
                x.FirstNameAr,
                x.MiddleNameAr,
                x.LastNameAr,
                x.FirstNameEn,
                x.MiddleNameEn,
                x.LastNameEn,
                x.DepartmentId,
                x.JobTitle
            })
            .ToDictionaryAsync(x => x.Id, ct);

        var departmentIds = users.Values
            .Where(x => x.DepartmentId.HasValue)
            .Select(x => x.DepartmentId!.Value)
            .Distinct()
            .ToList();

        var departments = await _db.Departments
            .AsNoTracking()
            .Where(d => departmentIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Name, ct);

        var custodyItems = await _db.CustodyItems
            .AsNoTracking()
            .Where(x => custodyItemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        return entities.Select(entity =>
        {
            users.TryGetValue(entity.UserId, out var user);
            custodyItems.TryGetValue(entity.CustodyItemId, out var custodyItem);

            return new EmployeeCustodyResponse
            {
                Id = entity.Id,
                UserId = entity.UserId,
                EmployeeCode = user?.EmployeeCode,
                UserFullNameAr = user == null ? null : BuildFullName(user.FirstNameAr, user.MiddleNameAr, user.LastNameAr),
                UserFullNameEn = user == null ? null : BuildFullName(user.FirstNameEn, user.MiddleNameEn, user.LastNameEn),
                DepartmentName = user?.DepartmentId.HasValue == true && departments.TryGetValue(user.DepartmentId.Value, out var departmentName)
                    ? departmentName
                    : null,
                JobTitle = user?.JobTitle,
                CustodyItemId = entity.CustodyItemId,
                CustodyItemName = custodyItem?.Name ?? string.Empty,
                Description = entity.Description,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }).ToList();
    }

    private static CustodyItemResponse Map(CustodyItem entity) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };

    private static string NormalizeRequiredText(string? value, string errorMessage)
    {
        var normalized = NormalizeOptionalText(value);
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException(errorMessage);

        return normalized;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? BuildFullName(string? firstName, string? middleName, string? lastName)
    {
        var fullName = string.Join(" ", new[] { firstName, middleName, lastName }
            .Where(x => !string.IsNullOrWhiteSpace(x)));

        return string.IsNullOrWhiteSpace(fullName) ? null : fullName;
    }
}
