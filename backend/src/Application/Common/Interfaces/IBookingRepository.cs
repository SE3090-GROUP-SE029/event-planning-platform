using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id);
    Task<Booking?> GetByQuotationIdAsync(Guid quotationId);
    Task<IReadOnlyList<Booking>> ListByRequesterAsync(Guid requestedByUserId);
    Task<IReadOnlyList<Booking>> ListByVendorIdAsync(Guid vendorId);
    Task<bool> HasActiveOverlapAsync(
        Guid vendorId,
        DateTime start,
        DateTime end,
        Guid? excludeBookingId = null);
    Task AddAsync(Booking booking);
    Task SaveChangesAsync();
}
