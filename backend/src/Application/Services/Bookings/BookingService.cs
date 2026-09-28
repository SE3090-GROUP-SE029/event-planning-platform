using Application.Common.Interfaces;
using Application.Dtos.Bookings;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Bookings;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookings;
    private readonly IQuotationRepository _quotations;
    private readonly IVendorRepository _vendors;
    private readonly IVendorOfferingRepository _offerings;
    private readonly IVendorAvailabilityRepository _availability;
    private readonly IVendorRatingRepository _ratings;
    private readonly IEventRepository _events;
    private readonly IUnitOfWork _unitOfWork;

    public BookingService(
        IBookingRepository bookings,
        IQuotationRepository quotations,
        IVendorRepository vendors,
        IVendorOfferingRepository offerings,
        IVendorAvailabilityRepository availability,
        IVendorRatingRepository ratings,
        IEventRepository events,
        IUnitOfWork unitOfWork)
    {
        _bookings = bookings;
        _quotations = quotations;
        _vendors = vendors;
        _offerings = offerings;
        _availability = availability;
        _ratings = ratings;
        _events = events;
        _unitOfWork = unitOfWork;
    }

    public async Task<BookingResponse> AcceptQuotationAsync(Guid plannerUserId, Guid quotationId)
    {
        Booking? created = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var quotation = await _quotations.GetByIdAsync(quotationId)
                ?? throw new KeyNotFoundException("Quotation not found.");

            if (quotation.RequestedByUserId != plannerUserId)
            {
                throw new UnauthorizedAccessException("You can only accept your own quotations.");
            }

            if (quotation.Status != QuotationStatus.QUOTED)
            {
                throw new InvalidOperationException("Only QUOTED quotations can be accepted.");
            }

            if (quotation.QuotedPrice is null)
            {
                throw new InvalidOperationException("Quotation does not have a quoted price.");
            }

            var eventEntity = await _events.GetByIdAsync(quotation.EventId)
                ?? throw new KeyNotFoundException("Event not found.");

            if (eventEntity.OwnerId != plannerUserId)
            {
                throw new UnauthorizedAccessException("You can only accept quotations for your own events.");
            }

            if (await _bookings.GetByQuotationIdAsync(quotation.Id) is not null)
            {
                throw new InvalidOperationException("This quotation already has a booking.");
            }

            var start = quotation.RequestedStartDateTime;
            var end = quotation.RequestedEndDateTime;

            if (await _bookings.HasActiveOverlapAsync(quotation.VendorId, start, end))
            {
                throw new ArgumentException("The vendor already has an active booking overlapping this period.");
            }

            await EnsureAvailableForPeriodAsync(quotation.VendorId, start, end);

            var now = DateTime.UtcNow;
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                QuotationId = quotation.Id,
                EventId = quotation.EventId,
                VendorId = quotation.VendorId,
                VendorServiceId = quotation.VendorServiceId,
                RequestedByUserId = quotation.RequestedByUserId,
                StartDateTime = start,
                EndDateTime = end,
                AgreedPrice = quotation.QuotedPrice.Value,
                VendorTerms = quotation.VendorTerms,
                Status = BookingStatus.CONFIRMED,
                CreatedAt = now
            };

            quotation.Status = QuotationStatus.ACCEPTED;
            quotation.UpdatedAt = now;

            await _bookings.AddAsync(booking);
            await ReserveAvailabilityAsync(quotation.VendorId, booking.Id, start, end);
            await _bookings.SaveChangesAsync();

            created = booking;
        });

        return await ToResponseAsync(created!);
    }

    public async Task<IReadOnlyList<BookingResponse>> ListMineAsync(Guid plannerUserId)
    {
        var items = await _bookings.ListByRequesterAsync(plannerUserId);
        return await MapManyAsync(items);
    }

    public async Task<IReadOnlyList<BookingResponse>> ListForVendorAsync(Guid vendorUserId)
    {
        var vendor = await RequireVendorAsync(vendorUserId);
        var items = await _bookings.ListByVendorIdAsync(vendor.Id);
        return await MapManyAsync(items);
    }

    public async Task<BookingResponse> GetByIdAsync(Guid bookingId, Guid userId, bool isVendor)
    {
        var booking = await _bookings.GetByIdAsync(bookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        if (booking.RequestedByUserId == userId)
        {
            return await ToResponseAsync(booking);
        }

        if (isVendor)
        {
            var vendor = await _vendors.GetByUserIdAsync(userId);
            if (vendor != null && vendor.Id == booking.VendorId)
            {
                return await ToResponseAsync(booking);
            }
        }

        throw new UnauthorizedAccessException("You are not authorized to view this booking.");
    }

    public async Task<BookingResponse> CompleteAsync(Guid vendorUserId, Guid bookingId)
    {
        var vendor = await RequireVendorAsync(vendorUserId);
        var booking = await _bookings.GetByIdAsync(bookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        if (booking.VendorId != vendor.Id)
        {
            throw new UnauthorizedAccessException("You can only complete bookings for your own business.");
        }

        if (booking.Status != BookingStatus.CONFIRMED)
        {
            throw new InvalidOperationException("Only CONFIRMED bookings can be completed.");
        }

        var now = DateTime.UtcNow;
        booking.Status = BookingStatus.COMPLETED;
        booking.CompletedAt = now;
        booking.UpdatedAt = now;

        await _bookings.SaveChangesAsync();
        return await ToResponseAsync(booking, vendor);
    }

    public async Task<BookingResponse> CancelAsync(
        Guid userId,
        Guid bookingId,
        CancelBookingRequest request,
        bool isVendor)
    {
        var booking = await _bookings.GetByIdAsync(bookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        var isPlannerOwner = booking.RequestedByUserId == userId;
        Vendor? vendor = null;

        if (!isPlannerOwner)
        {
            if (!isVendor)
            {
                throw new UnauthorizedAccessException("You are not authorized to cancel this booking.");
            }

            vendor = await _vendors.GetByUserIdAsync(userId);
            if (vendor == null || vendor.Id != booking.VendorId)
            {
                throw new UnauthorizedAccessException("You can only cancel bookings for your own business.");
            }
        }

        if (booking.Status != BookingStatus.CONFIRMED)
        {
            throw new InvalidOperationException("Only CONFIRMED bookings can be cancelled.");
        }

        var reason = (request.CancellationReason ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Cancellation reason is required.");
        }

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var now = DateTime.UtcNow;
            booking.Status = BookingStatus.CANCELLED;
            booking.CancellationReason = reason;
            booking.CancelledAt = now;
            booking.UpdatedAt = now;

            await ReleaseAvailabilityAsync(booking);
            await _bookings.SaveChangesAsync();
        });

        return await ToResponseAsync(booking, vendor);
    }

    private async Task EnsureAvailableForPeriodAsync(Guid vendorId, DateTime start, DateTime end)
    {
        var windows = await _availability.ListByVendorIdAsync(vendorId);

        var blocked = windows.Any(w =>
            !w.IsAvailable
            && w.StartDateTime < end
            && start < w.EndDateTime);

        if (blocked)
        {
            throw new ArgumentException("The vendor is marked unavailable for part of the requested period.");
        }

        var covered = windows.Any(w =>
            w.IsAvailable
            && w.StartDateTime <= start
            && end <= w.EndDateTime);

        if (!covered)
        {
            throw new ArgumentException(
                "The requested period is not fully covered by the vendor's available schedule.");
        }
    }

    private async Task ReserveAvailabilityAsync(Guid vendorId, Guid bookingId, DateTime start, DateTime end)
    {
        var windows = await _availability.ListByVendorIdAsync(vendorId);
        var covering = windows.FirstOrDefault(w =>
            w.IsAvailable
            && w.SourceBookingId == null
            && w.StartDateTime <= start
            && end <= w.EndDateTime);

        if (covering is null)
        {
            throw new ArgumentException(
                "The requested period is not fully covered by the vendor's available schedule.");
        }

        var windowStart = covering.StartDateTime;
        var windowEnd = covering.EndDateTime;
        var now = DateTime.UtcNow;

        _availability.Remove(covering);

        if (windowStart < start)
        {
            await _availability.AddAsync(new VendorAvailability
            {
                Id = Guid.NewGuid(),
                VendorId = vendorId,
                StartDateTime = windowStart,
                EndDateTime = start,
                IsAvailable = true,
                SourceBookingId = null,
                CreatedAt = now
            });
        }

        await _availability.AddAsync(new VendorAvailability
        {
            Id = Guid.NewGuid(),
            VendorId = vendorId,
            StartDateTime = start,
            EndDateTime = end,
            IsAvailable = false,
            SourceBookingId = bookingId,
            CreatedAt = now
        });

        if (end < windowEnd)
        {
            await _availability.AddAsync(new VendorAvailability
            {
                Id = Guid.NewGuid(),
                VendorId = vendorId,
                StartDateTime = end,
                EndDateTime = windowEnd,
                IsAvailable = true,
                SourceBookingId = null,
                CreatedAt = now
            });
        }
    }

    private async Task ReleaseAvailabilityAsync(Booking booking)
    {
        var locks = await _availability.ListBySourceBookingIdAsync(booking.Id);
        if (locks.Count == 0)
        {
            return;
        }

        var lockStart = locks.Min(l => l.StartDateTime);
        var lockEnd = locks.Max(l => l.EndDateTime);

        foreach (var item in locks)
        {
            _availability.Remove(item);
        }

        var remaining = (await _availability.ListByVendorIdAsync(booking.VendorId))
            .Where(a => a.IsAvailable && a.SourceBookingId == null)
            .OrderBy(a => a.StartDateTime)
            .ToList();

        var left = remaining.LastOrDefault(a => a.EndDateTime == lockStart);
        var right = remaining.FirstOrDefault(a => a.StartDateTime == lockEnd);

        var now = DateTime.UtcNow;

        if (left is not null && right is not null)
        {
            left.EndDateTime = right.EndDateTime;
            left.UpdatedAt = now;
            _availability.Remove(right);
        }
        else if (left is not null)
        {
            left.EndDateTime = lockEnd;
            left.UpdatedAt = now;
        }
        else if (right is not null)
        {
            right.StartDateTime = lockStart;
            right.UpdatedAt = now;
        }
        else
        {
            await _availability.AddAsync(new VendorAvailability
            {
                Id = Guid.NewGuid(),
                VendorId = booking.VendorId,
                StartDateTime = lockStart,
                EndDateTime = lockEnd,
                IsAvailable = true,
                SourceBookingId = null,
                CreatedAt = now
            });
        }
    }

    private async Task<Vendor> RequireVendorAsync(Guid userId)
    {
        return await _vendors.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Create a vendor profile before managing bookings.");
    }

    private async Task<IReadOnlyList<BookingResponse>> MapManyAsync(IReadOnlyList<Booking> items)
    {
        var results = new List<BookingResponse>(items.Count);
        foreach (var item in items)
        {
            results.Add(await ToResponseAsync(item));
        }

        return results;
    }

    private async Task<BookingResponse> ToResponseAsync(Booking booking, Vendor? vendor = null)
    {
        vendor ??= await _vendors.GetByIdAsync(booking.VendorId);
        var offering = await _offerings.GetByIdAsync(booking.VendorServiceId);
        var eventEntity = await _events.GetByIdAsync(booking.EventId);
        var hasReview = await _ratings.ExistsForBookingAsync(booking.Id);

        return new BookingResponse
        {
            Id = booking.Id,
            QuotationId = booking.QuotationId,
            EventId = booking.EventId,
            EventType = eventEntity?.EventType.ToString(),
            GuestCount = eventEntity?.GuestCount,
            EventPreferredDate = eventEntity?.PreferredDate,
            VendorId = booking.VendorId,
            VendorBusinessName = vendor?.BusinessName ?? "Unknown vendor",
            VendorServiceId = booking.VendorServiceId,
            ServiceName = offering?.ServiceName ?? "Unknown service",
            RequestedByUserId = booking.RequestedByUserId,
            StartDateTime = booking.StartDateTime,
            EndDateTime = booking.EndDateTime,
            AgreedPrice = booking.AgreedPrice,
            VendorTerms = booking.VendorTerms,
            Status = booking.Status.ToString(),
            CancellationReason = booking.CancellationReason,
            CreatedAt = booking.CreatedAt,
            UpdatedAt = booking.UpdatedAt,
            CompletedAt = booking.CompletedAt,
            CancelledAt = booking.CancelledAt,
            HasReview = hasReview
        };
    }
}
