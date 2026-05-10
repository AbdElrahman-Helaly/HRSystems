using internalEmployee.Auth.Contracts;
using internalEmployee.Data;
using Microsoft.EntityFrameworkCore;

namespace internalEmployee.Services.InsuranceCompany;

public sealed class InsuranceCompanyService : IInsuranceCompanyService
{
    private readonly AppDbContext _context;

    public InsuranceCompanyService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Data.Entities.InsuranceCompany>> GetAllAsync(CancellationToken ct)
    {
        return await _context.InsuranceCompanies
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }

    public async Task<Data.Entities.InsuranceCompany?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.InsuranceCompanies
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<Data.Entities.InsuranceCompany> CreateAsync(CreateInsuranceCompanyRequest request, CancellationToken ct)
    {
        // Check if name already exists
        var nameExists = await _context.InsuranceCompanies
            .AnyAsync(x => x.Name == request.Name.Trim(), ct);
        
        if (nameExists)
            throw new InvalidOperationException("Insurance company with this name already exists.");

        var company = new Data.Entities.InsuranceCompany
        {
            Name = request.Name.Trim(),
            NameAr = request.NameAr?.Trim(),
            IsActive = request.IsActive
        };

        _context.InsuranceCompanies.Add(company);
        await _context.SaveChangesAsync(ct);

        return company;
    }

    public async Task<Data.Entities.InsuranceCompany> UpdateAsync(int id, UpdateInsuranceCompanyRequest request, CancellationToken ct)
    {
        var company = await _context.InsuranceCompanies
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (company == null)
            throw new InvalidOperationException("Insurance company not found.");

        // Check if new name already exists (excluding current company)
        var nameExists = await _context.InsuranceCompanies
            .AnyAsync(x => x.Name == request.Name.Trim() && x.Id != id, ct);
        
        if (nameExists)
            throw new InvalidOperationException("Insurance company with this name already exists.");

        company.Name = request.Name.Trim();
        company.NameAr = request.NameAr?.Trim();
        company.IsActive = request.IsActive;

        await _context.SaveChangesAsync(ct);

        return company;
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var company = await _context.InsuranceCompanies
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (company == null)
            throw new InvalidOperationException("Insurance company not found.");

        // Check if any users are linked to this company
        var hasUsers = await _context.Users
            .AnyAsync(u => u.InsuranceCompanyId == id, ct);

        if (hasUsers)
            throw new InvalidOperationException("Cannot delete insurance company. It is linked to one or more employees.");

        _context.InsuranceCompanies.Remove(company);
        await _context.SaveChangesAsync(ct);
    }
}
