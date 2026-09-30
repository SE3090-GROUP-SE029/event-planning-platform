using Application.Dtos.Bookings;
using Application.Dtos.Quotations;
using Application.Services.Bookings;
using Application.Services.Quotations;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Backend.UnitTests;

public class BookingServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static BookingService CreateBookingService(AppDbContext db) =>
        new(
            new BookingRepository(db),
            new QuotationRepository(db),
            new VendorRepository(db),
            new VendorOfferingRepository(db),
            new VendorAvailabilityRepository(db),
            new VendorRatingRepository(db),
            new EventRepository(db),
            new EfUnitOfWork(db));

    private static QuotationService CreateQuotationService(AppDbContext db) =>
        new(
            new QuotationRepository(db),
            new VendorRepository(db),
            new VendorOfferingRepository(db),
            new VendorAvailabilityRepository(db),
            new EventRepository(db));

    private static async Task<(
        Vendor Vendor,
        VendorOffering Offering,
        Event Event,
        Quotation Quotation)> SeedQuotedAsync(
        AppDbContext db,
        Guid plannerUserId,
        DateTime? start = null,
        DateTime? end = null)
    {
        var bookingStart = start ?? new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
        var bookingEnd = end ?? new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc);

        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BusinessName = "KG Photography",
            Category = BusinessCategory.PHOTOGRAPHY,
            ContactEmail = "kg@photo.test",
            ContactPhone = "0770001111",
            Address = "12 Studio Road",
            Status = VendorStatus.APPROVED,
            CreatedAt = DateTime.UtcNow
        };
        var offering = new VendorOffering
        {
            Id = Guid.NewGuid(),
            VendorId = vendor.Id,
            ServiceName = "Wedding Package",
            Description = "Full day",
            Price = 50000m,
            PricingType = PricingType.FIXED,
            CreatedAt = DateTime.UtcNow
        };
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            OwnerId = plannerUserId,
            EventType = EventType.WEDDING,
            GuestCount = 100,
            Budget = 200000m,
            PreferredVenue = "Garden Hall",
            PreferredDate = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            EventDuration = TimeSpan.FromHours(6),
            Requirements = "Outdoor preferred",
            Status = EventStatus.DRAFT,
            CreatedAt = DateTime.UtcNow
        };
        var availability = new VendorAvailability
        {
            Id = Guid.NewGuid(),
            VendorId = vendor.Id,
            StartDateTime = new DateTime(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc),
            EndDateTime = new DateTime(2026, 11, 1, 20, 0, 0, DateTimeKind.Utc),
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };
        var quotation = new Quotation
        {
            Id = Guid.NewGuid(),
            EventId = eventEntity.Id,
            VendorId = vendor.Id,
            VendorServiceId = offering.Id,
            RequestedByUserId = plannerUserId,
            RequestedStartDateTime = bookingStart,
            RequestedEndDateTime = bookingEnd,
            QuotedPrice = 75000m,
            VendorTerms = "50% deposit",
            Status = QuotationStatus.QUOTED,
            RequestedAt = DateTime.UtcNow.AddDays(-1),
            RespondedAt = DateTime.UtcNow
        };

        db.Vendors.Add(vendor);
        db.VendorOfferings.Add(offering);
        db.Events.Add(eventEntity);
        db.VendorAvailabilities.Add(availability);
        db.Quotations.Add(quotation);
        await db.SaveChangesAsync();
        return (vendor, offering, eventEntity, quotation);
    }

    [Fact]
    public async Task AcceptQuotationAsync_CreatesConfirmedBookingAndLocksAvailability()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (_, _, _, quotation) = await SeedQuotedAsync(db, plannerId);
        var service = CreateBookingService(db);

        var result = await service.AcceptQuotationAsync(plannerId, quotation.Id);

        Assert.Equal(nameof(BookingStatus.CONFIRMED), result.Status);
        Assert.Equal(quotation.Id, result.QuotationId);
        Assert.Equal(75000m, result.AgreedPrice);
        Assert.Equal("50% deposit", result.VendorTerms);
        Assert.Single(db.Bookings);

        var updatedQuotation = await db.Quotations.FindAsync(quotation.Id);
        Assert.Equal(QuotationStatus.ACCEPTED, updatedQuotation!.Status);

        var windows = db.VendorAvailabilities.OrderBy(a => a.StartDateTime).ToList();
        Assert.Equal(3, windows.Count);
        Assert.True(windows[0].IsAvailable);
        Assert.Null(windows[0].SourceBookingId);
        Assert.False(windows[1].IsAvailable);
        Assert.Equal(result.Id, windows[1].SourceBookingId);
        Assert.Equal(quotation.RequestedStartDateTime, windows[1].StartDateTime);
        Assert.Equal(quotation.RequestedEndDateTime, windows[1].EndDateTime);
        Assert.True(windows[2].IsAvailable);
        Assert.Null(windows[2].SourceBookingId);
    }

    [Fact]
    public async Task AcceptQuotationAsync_RejectsNonQuoted()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (_, _, _, quotation) = await SeedQuotedAsync(db, plannerId);
        quotation.Status = QuotationStatus.REQUESTED;
        await db.SaveChangesAsync();
        var service = CreateBookingService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AcceptQuotationAsync(plannerId, quotation.Id));
    }

    [Fact]
    public async Task AcceptQuotationAsync_RejectsDuplicateAccept()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (_, _, _, quotation) = await SeedQuotedAsync(db, plannerId);
        var service = CreateBookingService(db);

        await service.AcceptQuotationAsync(plannerId, quotation.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AcceptQuotationAsync(plannerId, quotation.Id));
    }

    [Fact]
    public async Task AcceptQuotationAsync_RejectsWhenNotOwner()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (_, _, _, quotation) = await SeedQuotedAsync(db, plannerId);
        var service = CreateBookingService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.AcceptQuotationAsync(Guid.NewGuid(), quotation.Id));
    }

    [Fact]
    public async Task CompleteAsync_MarksCompleted()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (vendor, _, _, quotation) = await SeedQuotedAsync(db, plannerId);
        var service = CreateBookingService(db);
        var booking = await service.AcceptQuotationAsync(plannerId, quotation.Id);

        var completed = await service.CompleteAsync(vendor.UserId, booking.Id);

        Assert.Equal(nameof(BookingStatus.COMPLETED), completed.Status);
        Assert.NotNull(completed.CompletedAt);
    }

    [Fact]
    public async Task CancelAsync_ByPlanner_ReleasesAvailabilityLock()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (_, _, _, quotation) = await SeedQuotedAsync(db, plannerId);
        var service = CreateBookingService(db);
        var booking = await service.AcceptQuotationAsync(plannerId, quotation.Id);

        var cancelled = await service.CancelAsync(
            plannerId,
            booking.Id,
            new CancelBookingRequest { CancellationReason = "Plans changed" },
            isVendor: false);

        Assert.Equal(nameof(BookingStatus.CANCELLED), cancelled.Status);
        Assert.Equal("Plans changed", cancelled.CancellationReason);
        Assert.DoesNotContain(db.VendorAvailabilities, a => a.SourceBookingId == booking.Id);

        var windows = db.VendorAvailabilities.Where(a => a.IsAvailable).OrderBy(a => a.StartDateTime).ToList();
        Assert.Single(windows);
        Assert.Equal(new DateTime(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc), windows[0].StartDateTime);
        Assert.Equal(new DateTime(2026, 11, 1, 20, 0, 0, DateTimeKind.Utc), windows[0].EndDateTime);
    }

    [Fact]
    public async Task CancelAsync_RequiresReason()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (_, _, _, quotation) = await SeedQuotedAsync(db, plannerId);
        var service = CreateBookingService(db);
        var booking = await service.AcceptQuotationAsync(plannerId, quotation.Id);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CancelAsync(
                plannerId,
                booking.Id,
                new CancelBookingRequest { CancellationReason = "  " },
                isVendor: false));
    }

    [Fact]
    public async Task CancelAsync_RejectsCompletedBooking()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (vendor, _, _, quotation) = await SeedQuotedAsync(db, plannerId);
        var service = CreateBookingService(db);
        var booking = await service.AcceptQuotationAsync(plannerId, quotation.Id);
        await service.CompleteAsync(vendor.UserId, booking.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CancelAsync(
                plannerId,
                booking.Id,
                new CancelBookingRequest { CancellationReason = "Too late" },
                isVendor: false));
    }

    [Fact]
    public async Task AcceptQuotationAsync_RejectsOverlappingActiveBooking()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (vendor, offering, eventEntity, quotation) = await SeedQuotedAsync(db, plannerId);
        var service = CreateBookingService(db);
        await service.AcceptQuotationAsync(plannerId, quotation.Id);

        // Restore a covering available window for a second overlapping quote (simulate second request before lock)
        // After first accept, middle is locked — create a new quoted request that would overlap.
        var second = new Quotation
        {
            Id = Guid.NewGuid(),
            EventId = eventEntity.Id,
            VendorId = vendor.Id,
            VendorServiceId = offering.Id,
            RequestedByUserId = plannerId,
            RequestedStartDateTime = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc),
            RequestedEndDateTime = new DateTime(2026, 11, 1, 16, 0, 0, DateTimeKind.Utc),
            QuotedPrice = 80000m,
            Status = QuotationStatus.QUOTED,
            RequestedAt = DateTime.UtcNow,
            RespondedAt = DateTime.UtcNow
        };
        db.Quotations.Add(second);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AcceptQuotationAsync(plannerId, second.Id));
    }

    [Fact]
    public async Task EndToEnd_QuotationRespondThenAccept()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (vendor, offering, eventEntity, _) = await SeedQuotedAsync(db, plannerId);
        // Replace with REQUESTED path via services
        db.Quotations.RemoveRange(db.Quotations);
        await db.SaveChangesAsync();

        var quotationService = CreateQuotationService(db);
        var bookingService = CreateBookingService(db);

        var created = await quotationService.CreateAsync(plannerId, new CreateQuotationRequest
        {
            VendorId = vendor.Id,
            VendorServiceId = offering.Id,
            EventId = eventEntity.Id,
            RequestedStartDateTime = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
            RequestedEndDateTime = new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc)
        });

        await quotationService.RespondAsync(vendor.UserId, created.Id, new RespondToQuotationRequest
        {
            QuotedPrice = 60000m,
            VendorTerms = "Net 7"
        });

        var booking = await bookingService.AcceptQuotationAsync(plannerId, created.Id);
        Assert.Equal(nameof(BookingStatus.CONFIRMED), booking.Status);
        Assert.Equal(60000m, booking.AgreedPrice);
    }
}
