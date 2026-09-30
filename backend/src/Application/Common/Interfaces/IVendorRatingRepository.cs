using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IVendorRatingRepository
{
    Task<VendorRating?> GetByBookingIdAsync(Guid bookingId);
    Task<bool> ExistsForBookingAsync(Guid bookingId);
    Task<(decimal? AverageRating, int ReviewCount)> GetAggregateForVendorAsync(Guid vendorId);
    Task<IReadOnlyDictionary<Guid, (decimal AverageRating, int ReviewCount)>> GetAggregatesForVendorsAsync(
        IEnumerable<Guid> vendorIds);
    Task AddAsync(VendorRating rating);
    Task SaveChangesAsync();
}
