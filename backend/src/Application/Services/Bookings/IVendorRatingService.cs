using Application.Dtos.Bookings;

namespace Application.Services.Bookings;

public interface IVendorRatingService
{
    Task<VendorRatingResponse> CreateAsync(Guid plannerUserId, Guid bookingId, CreateVendorRatingRequest request);
    Task<VendorRatingResponse> GetByBookingIdAsync(Guid bookingId, Guid userId, bool isVendor);
}
