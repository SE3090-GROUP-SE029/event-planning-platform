using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class VendorOfferingRepository : IVendorOfferingRepository
{
    private readonly AppDbContext _db;

    public VendorOfferingRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<VendorOffering>> ListByVendorIdAsync(Guid vendorId) =>
        await _db.VendorOfferings
            .Where(o => o.VendorId == vendorId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public Task<VendorOffering?> GetByIdAsync(Guid id) =>
        _db.VendorOfferings.FirstOrDefaultAsync(o => o.Id == id);

    public async Task AddAsync(VendorOffering offering) =>
        await _db.VendorOfferings.AddAsync(offering);

    public void Remove(VendorOffering offering) =>
        _db.VendorOfferings.Remove(offering);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
