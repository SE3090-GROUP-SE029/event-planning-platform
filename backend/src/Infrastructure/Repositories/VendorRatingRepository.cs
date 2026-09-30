using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class VendorRatingRepository : IVendorRatingRepository
{
    private readonly AppDbContext _db;

    public VendorRatingRepository(AppDbContext db) => _db = db;

    public Task<VendorRating?> GetByBookingIdAsync(Guid bookingId) =>
        _db.VendorRatings.FirstOrDefaultAsync(r => r.BookingId == bookingId);

    public Task<bool> ExistsForBookingAsync(Guid bookingId) =>
        _db.VendorRatings.AnyAsync(r => r.BookingId == bookingId);

    public async Task<(decimal? AverageRating, int ReviewCount)> GetAggregateForVendorAsync(Guid vendorId)
    {
        var query = _db.VendorRatings.AsNoTracking().Where(r => r.VendorId == vendorId);
        var count = await query.CountAsync();
        if (count == 0)
        {
            return (null, 0);
        }

        var average = await query.AverageAsync(r => (decimal)r.Rating);
        return (Math.Round(average, 2, MidpointRounding.AwayFromZero), count);
    }

    public async Task<IReadOnlyDictionary<Guid, (decimal AverageRating, int ReviewCount)>> GetAggregatesForVendorsAsync(
        IEnumerable<Guid> vendorIds)
    {
        var ids = vendorIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, (decimal, int)>();
        }

        var rows = await _db.VendorRatings
            .AsNoTracking()
            .Where(r => ids.Contains(r.VendorId))
            .GroupBy(r => r.VendorId)
            .Select(g => new
            {
                VendorId = g.Key,
                Average = g.Average(r => (decimal)r.Rating),
                Count = g.Count()
            })
            .ToListAsync();

        return rows.ToDictionary(
            r => r.VendorId,
            r => (Math.Round(r.Average, 2, MidpointRounding.AwayFromZero), r.Count));
    }

    public async Task AddAsync(VendorRating rating) =>
        await _db.VendorRatings.AddAsync(rating);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
