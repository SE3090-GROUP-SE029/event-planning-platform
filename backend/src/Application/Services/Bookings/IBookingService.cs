using Application.Dtos.Bookings;

namespace Application.Services.Bookings;

public interface IBookingService
{
    Task<BookingResponse> AcceptQuotationAsync(Guid plannerUserId, Guid quotationId);
    Task<IReadOnlyList<BookingResponse>> ListMineAsync(Guid plannerUserId);
    Task<IReadOnlyList<BookingResponse>> ListForVendorAsync(Guid vendorUserId);
    Task<BookingResponse> GetByIdAsync(Guid bookingId, Guid userId, bool isVendor);
    Task<BookingResponse> CompleteAsync(Guid vendorUserId, Guid bookingId);
    Task<BookingResponse> CancelAsync(Guid userId, Guid bookingId, CancelBookingRequest request, bool isVendor);
}
