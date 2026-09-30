using Application.Dtos.Bookings;
using Application.Services.Bookings;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Backend.UnitTests;

public class VendorRatingServiceTests
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

    private static VendorRatingService CreateService(AppDbContext db) =>
        new(
            new VendorRatingRepository(db),
            new BookingRepository(db),
            new VendorRepository(db));

    private static async Task<(Vendor Vendor, Booking Booking)> SeedCompletedBookingAsync(
        AppDbContext db,
        Guid plannerUserId,
        Guid? vendorUserId = null)
    {
        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            UserId = vendorUserId ?? Guid.NewGuid(),
            BusinessName = "Rated Studio",
            Category = BusinessCategory.PHOTOGRAPHY,
            ContactEmail = "rated@test.local",
            ContactPhone = "0770000000",
            Address = "1 Test Street",
            Status = VendorStatus.APPROVED,
            CreatedAt = DateTime.UtcNow
        };

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            QuotationId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            VendorId = vendor.Id,
            VendorServiceId = Guid.NewGuid(),
            RequestedByUserId = plannerUserId,
            StartDateTime = DateTime.UtcNow.AddDays(-2),
            EndDateTime = DateTime.UtcNow.AddDays(-1),
            AgreedPrice = 15000m,
            Status = BookingStatus.COMPLETED,
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            CompletedAt = DateTime.UtcNow.AddHours(-1)
        };

        db.Vendors.Add(vendor);
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return (vendor, booking);
    }

    [Fact]
    public async Task CreateAsync_CreatesReview_ForCompletedOwnedBooking()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (_, booking) = await SeedCompletedBookingAsync(db, plannerId);
        var service = CreateService(db);

        var result = await service.CreateAsync(plannerId, booking.Id, new CreateVendorRatingRequest
        {
            Rating = 5,
            Comment = "Excellent service"
        });

        Assert.Equal(5, result.Rating);
        Assert.Equal("Excellent service", result.Comment);
        Assert.Equal(booking.Id, result.BookingId);
        Assert.True(await db.VendorRatings.AnyAsync(r => r.BookingId == booking.Id));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenRatingOutOfRange()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (_, booking) = await SeedCompletedBookingAsync(db, plannerId);
        var service = CreateService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(plannerId, booking.Id, new CreateVendorRatingRequest { Rating = 0 }));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenNotOwner()
    {
        using var db = CreateDb();
        var (_, booking) = await SeedCompletedBookingAsync(db, Guid.NewGuid());
        var service = CreateService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateAsync(Guid.NewGuid(), booking.Id, new CreateVendorRatingRequest { Rating = 4 }));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenBookingNotCompleted()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (_, booking) = await SeedCompletedBookingAsync(db, plannerId);
        booking.Status = BookingStatus.CONFIRMED;
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(plannerId, booking.Id, new CreateVendorRatingRequest { Rating = 4 }));
        Assert.Contains("COMPLETED", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenVendorReviewsSelf()
    {
        using var db = CreateDb();
        var sameUser = Guid.NewGuid();
        var (_, booking) = await SeedCompletedBookingAsync(db, sameUser, vendorUserId: sameUser);
        var service = CreateService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateAsync(sameUser, booking.Id, new CreateVendorRatingRequest { Rating = 5 }));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenReviewAlreadyExists()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (_, booking) = await SeedCompletedBookingAsync(db, plannerId);
        var service = CreateService(db);
        await service.CreateAsync(plannerId, booking.Id, new CreateVendorRatingRequest { Rating = 4 });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(plannerId, booking.Id, new CreateVendorRatingRequest { Rating = 5 }));
        Assert.Contains("already", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetByBookingIdAsync_ReturnsReview_ForPlannerOwner()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (_, booking) = await SeedCompletedBookingAsync(db, plannerId);
        var service = CreateService(db);
        await service.CreateAsync(plannerId, booking.Id, new CreateVendorRatingRequest
        {
            Rating = 3,
            Comment = "Okay"
        });

        var result = await service.GetByBookingIdAsync(booking.Id, plannerId, isVendor: false);

        Assert.Equal(3, result.Rating);
        Assert.Equal("Okay", result.Comment);
    }
}
