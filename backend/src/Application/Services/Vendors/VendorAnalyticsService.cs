using Application.Common.Interfaces;
using Application.Dtos.Vendors;
using Domain.Enums;

namespace Application.Services.Vendors;

public class VendorAnalyticsService : IVendorAnalyticsService
{
    private const int RecentBookingsLimit = 5;

    private readonly IVendorRepository _vendors;
    private readonly IBookingRepository _bookings;
    private readonly IQuotationRepository _quotations;
    private readonly IVendorOfferingRepository _offerings;
    private readonly IVendorRatingRepository _ratings;

    public VendorAnalyticsService(
        IVendorRepository vendors,
        IBookingRepository bookings,
        IQuotationRepository quotations,
        IVendorOfferingRepository offerings,
        IVendorRatingRepository ratings)
    {
        _vendors = vendors;
        _bookings = bookings;
        _quotations = quotations;
        _offerings = offerings;
        _ratings = ratings;
    }

    public async Task<VendorAnalyticsResponse> GetMyAnalyticsAsync(Guid vendorUserId)
    {
        var vendor = await _vendors.GetByUserIdAsync(vendorUserId)
            ?? throw new KeyNotFoundException("Create a vendor profile before viewing analytics.");

        var bookings = await _bookings.ListByVendorIdAsync(vendor.Id);
        var quotations = await _quotations.ListByVendorIdAsync(vendor.Id);
        var (averageRating, reviewCount) = await _ratings.GetAggregateForVendorAsync(vendor.Id);

        var completed = bookings.Where(b => b.Status == BookingStatus.COMPLETED).ToList();
        var recent = bookings.Take(RecentBookingsLimit).ToList();
        var offeringNames = new Dictionary<Guid, string>();

        foreach (var booking in recent)
        {
            if (offeringNames.ContainsKey(booking.VendorServiceId))
            {
                continue;
            }

            var offering = await _offerings.GetByIdAsync(booking.VendorServiceId);
            offeringNames[booking.VendorServiceId] = offering?.ServiceName ?? "Unknown service";
        }

        var recentItems = new List<VendorAnalyticsBookingItemResponse>(recent.Count);
        foreach (var booking in recent)
        {
            recentItems.Add(new VendorAnalyticsBookingItemResponse
            {
                Id = booking.Id,
                ServiceName = offeringNames.GetValueOrDefault(booking.VendorServiceId, "Unknown service"),
                Status = booking.Status.ToString(),
                StartDateTime = booking.StartDateTime,
                EndDateTime = booking.EndDateTime,
                AgreedPrice = booking.AgreedPrice,
                CreatedAt = booking.CreatedAt,
                HasReview = await _ratings.ExistsForBookingAsync(booking.Id)
            });
        }

        return new VendorAnalyticsResponse
        {
            TotalBookings = bookings.Count,
            ConfirmedBookings = bookings.Count(b => b.Status == BookingStatus.CONFIRMED),
            CompletedBookings = completed.Count,
            CancelledBookings = bookings.Count(b => b.Status == BookingStatus.CANCELLED),
            TotalRevenue = completed.Sum(b => b.AgreedPrice),
            AverageRating = averageRating,
            ReviewCount = reviewCount,
            TotalQuotations = quotations.Count,
            QuotationsAccepted = quotations.Count(q => q.Status == QuotationStatus.ACCEPTED),
            RecentBookings = recentItems
        };
    }
}
