using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class VendorAvailabilityRepository : IVendorAvailabilityRepository
{
    private readonly AppDbContext _db;

    public VendorAvailabilityRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<VendorAvailability>> ListByVendorIdAsync(Guid vendorId) =>
        await _db.VendorAvailabilities
            .Where(a => a.VendorId == vendorId)
            .OrderBy(a => a.StartDateTime)
            .ToListAsync();

    public Task<VendorAvailability?> GetByIdAsync(Guid id) =>
        _db.VendorAvailabilities.FirstOrDefaultAsync(a => a.Id == id);

    public Task<bool> HasOverlapAsync(Guid vendorId, DateTime start, DateTime end, Guid? excludeId = null)
    {
        // Overlap: existing.Start < new.End AND new.Start < existing.End
        // Adjacent (existing.End == new.Start) is NOT an overlap.
        var query = _db.VendorAvailabilities
            .Where(a => a.VendorId == vendorId
                        && a.StartDateTime < end
                        && start < a.EndDateTime);

        if (excludeId.HasValue)
        {
            query = query.Where(a => a.Id != excludeId.Value);
        }

        return query.AnyAsync();
    }

    public async Task AddAsync(VendorAvailability availability) =>
        await _db.VendorAvailabilities.AddAsync(availability);

    public void Remove(VendorAvailability availability) =>
        _db.VendorAvailabilities.Remove(availability);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
