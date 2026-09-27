using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private static readonly BookingStatus[] ActiveStatuses =
    [
        BookingStatus.CONFIRMED,
        BookingStatus.IN_PROGRESS
    ];

    private readonly AppDbContext _db;

    public BookingRepository(AppDbContext db) => _db = db;

    public Task<Booking?> GetByIdAsync(Guid id) =>
        _db.Bookings.FirstOrDefaultAsync(b => b.Id == id);

    public Task<Booking?> GetByQuotationIdAsync(Guid quotationId) =>
        _db.Bookings.FirstOrDefaultAsync(b => b.QuotationId == quotationId);

    public async Task<IReadOnlyList<Booking>> ListByRequesterAsync(Guid requestedByUserId) =>
        await _db.Bookings
            .Where(b => b.RequestedByUserId == requestedByUserId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<Booking>> ListByVendorIdAsync(Guid vendorId) =>
        await _db.Bookings
            .Where(b => b.VendorId == vendorId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

    public Task<bool> HasActiveOverlapAsync(
        Guid vendorId,
        DateTime start,
        DateTime end,
        Guid? excludeBookingId = null)
    {
        var query = _db.Bookings
            .Where(b => b.VendorId == vendorId
                        && ActiveStatuses.Contains(b.Status)
                        && b.StartDateTime < end
                        && start < b.EndDateTime);

        if (excludeBookingId.HasValue)
        {
            query = query.Where(b => b.Id != excludeBookingId.Value);
        }

        return query.AnyAsync();
    }

    public async Task AddAsync(Booking booking) =>
        await _db.Bookings.AddAsync(booking);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
