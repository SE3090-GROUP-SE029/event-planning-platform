using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class VendorRecommendationRepository : IVendorRecommendationRepository
{
    private readonly AppDbContext _db;

    public VendorRecommendationRepository(AppDbContext db) => _db = db;

    public async Task AddRunAsync(VendorRecommendationRun run, CancellationToken cancellationToken = default) =>
        await _db.VendorRecommendationRuns.AddAsync(run, cancellationToken);

    public Task<VendorRecommendationRun?> GetLatestForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default) =>
        _db.VendorRecommendationRuns
            .AsNoTracking()
            .Include(r => r.Items)
            .Where(r => r.EventId == eventId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Vendor>> ListApprovedByCategoriesAsync(
        IReadOnlyCollection<BusinessCategory> categories,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Vendors
            .AsNoTracking()
            .Where(v => v.Status == VendorStatus.APPROVED);

        if (categories.Count > 0)
            query = query.Where(v => categories.Contains(v.Category));

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VendorOffering>> ListOfferingsForVendorsAsync(
        IEnumerable<Guid> vendorIds,
        CancellationToken cancellationToken = default)
    {
        var ids = vendorIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        return await _db.VendorOfferings
            .AsNoTracking()
            .Where(o => ids.Contains(o.VendorId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VendorAvailability>> ListAvailabilityForVendorsAsync(
        IEnumerable<Guid> vendorIds,
        CancellationToken cancellationToken = default)
    {
        var ids = vendorIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        return await _db.VendorAvailabilities
            .AsNoTracking()
            .Where(a => ids.Contains(a.VendorId))
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
