using Application.Common.Interfaces;
using Application.Dtos.Bookings;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Bookings;

public class VendorRatingService : IVendorRatingService
{
    private readonly IVendorRatingRepository _ratings;
    private readonly IBookingRepository _bookings;
    private readonly IVendorRepository _vendors;

    public VendorRatingService(
        IVendorRatingRepository ratings,
        IBookingRepository bookings,
        IVendorRepository vendors)
    {
        _ratings = ratings;
        _bookings = bookings;
        _vendors = vendors;
    }

    public async Task<VendorRatingResponse> CreateAsync(
        Guid plannerUserId,
        Guid bookingId,
        CreateVendorRatingRequest request)
    {
        if (request.Rating is < 1 or > 5)
        {
            throw new ArgumentException("Rating must be between 1 and 5.");
        }

        var comment = string.IsNullOrWhiteSpace(request.Comment)
            ? null
            : request.Comment.Trim();

        if (comment is { Length: > 2000 })
        {
            throw new ArgumentException("Comment must be at most 2000 characters.");
        }

        var booking = await _bookings.GetByIdAsync(bookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        if (booking.RequestedByUserId != plannerUserId)
        {
            throw new UnauthorizedAccessException("You can only review your own bookings.");
        }

        if (booking.Status != BookingStatus.COMPLETED)
        {
            throw new InvalidOperationException("Only COMPLETED bookings can be reviewed.");
        }

        var vendor = await _vendors.GetByIdAsync(booking.VendorId)
            ?? throw new KeyNotFoundException("Vendor not found.");

        if (vendor.UserId == plannerUserId)
        {
            throw new UnauthorizedAccessException("Vendors cannot review their own bookings.");
        }

        if (await _ratings.ExistsForBookingAsync(booking.Id))
        {
            throw new InvalidOperationException("This booking already has a review.");
        }

        var rating = new VendorRating
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            VendorId = booking.VendorId,
            ReviewerUserId = plannerUserId,
            Rating = request.Rating,
            Comment = comment,
            CreatedAt = DateTime.UtcNow
        };

        await _ratings.AddAsync(rating);
        await _ratings.SaveChangesAsync();
        return ToResponse(rating);
    }

    public async Task<VendorRatingResponse> GetByBookingIdAsync(Guid bookingId, Guid userId, bool isVendor)
    {
        var booking = await _bookings.GetByIdAsync(bookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        var isPlannerOwner = booking.RequestedByUserId == userId;
        if (!isPlannerOwner)
        {
            if (!isVendor)
            {
                throw new UnauthorizedAccessException("You are not authorized to view this review.");
            }

            var vendor = await _vendors.GetByUserIdAsync(userId);
            if (vendor == null || vendor.Id != booking.VendorId)
            {
                throw new UnauthorizedAccessException("You are not authorized to view this review.");
            }
        }

        var rating = await _ratings.GetByBookingIdAsync(bookingId)
            ?? throw new KeyNotFoundException("Review not found.");

        return ToResponse(rating);
    }

    private static VendorRatingResponse ToResponse(VendorRating rating) => new()
    {
        Id = rating.Id,
        BookingId = rating.BookingId,
        VendorId = rating.VendorId,
        ReviewerUserId = rating.ReviewerUserId,
        Rating = rating.Rating,
        Comment = rating.Comment,
        CreatedAt = rating.CreatedAt
    };
}
