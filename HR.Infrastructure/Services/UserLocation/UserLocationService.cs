using internalEmployee.Auth.Contracts;
using internalEmployee.Data;
using internalEmployee.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace internalEmployee.Services.UserLocation;

public sealed class UserLocationService : IUserLocationService
{
    private readonly AppDbContext _db;

    public UserLocationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserLocationResponse> CreateAsync(CreateUserLocationRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var entity = new Data.Entities.UserLocation
        {
            UserId = request.UserId,
            Name = request.Name,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RadiusMeters = request.RadiusMeters,
            IsActive = request.IsActive,
            CreatedAt = DateTime.Now
        };

        _db.UserLocations.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Map(entity);
    }

    public async Task<UserLocationResponse> UpdateAsync(int id, UpdateUserLocationRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var entity = await _db.UserLocations.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity == null)
            throw new InvalidOperationException("Location not found.");

        entity.Name = request.Name;
        entity.Latitude = request.Latitude;
        entity.Longitude = request.Longitude;
        entity.RadiusMeters = request.RadiusMeters;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync(ct);

        return Map(entity);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var entity = await _db.UserLocations.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity == null)
            return false;

        _db.UserLocations.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<UserLocationResponse>> GetByUserAsync(Guid userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var items = await _db.UserLocations
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return items.Select(Map).ToList();
    }

    public async Task<UserLocationResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var entity = await _db.UserLocations.FirstOrDefaultAsync(x => x.Id == id, ct);
        return entity == null ? null : Map(entity);
    }

    private static UserLocationResponse Map(Data.Entities.UserLocation entity) =>
        new()
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Name = entity.Name,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            RadiusMeters = entity.RadiusMeters,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
}

