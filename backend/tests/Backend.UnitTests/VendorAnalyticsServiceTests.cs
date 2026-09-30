using Application.Services.Vendors;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Backend.UnitTests;

public class VendorAnalyticsServiceTests
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

    private static VendorAnalyticsService CreateService(AppDbContext db) =>
        new(
            new VendorRepository(db),
            new BookingRepository(db),
            new QuotationRepository(db),
            new VendorOfferingRepository(db),
            new VendorRatingRepository(db));

    private static async Task<(Vendor Vendor, VendorOffering Offering)> SeedVendorAsync(
        AppDbContext db,
        Guid userId,
        string businessName = "Analytics Vendor")
    {
        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BusinessName = businessName,
            Category = BusinessCategory.PHOTOGRAPHY,
            ContactEmail = $"{businessName.Replace(" ", "").ToLowerInvariant()}@test.local",
            ContactPhone = "0770000000",
            Address = "1 Analytics Road",
            Status = VendorStatus.APPROVED,
            CreatedAt = DateTime.UtcNow
        };

        var offering = new VendorOffering
        {
            Id = Guid.NewGuid(),
            VendorId = vendor.Id,
            ServiceName = "Wedding Package",
            Price = 50000m,
            PricingType = PricingType.FIXED,
            CreatedAt = DateTime.UtcNow
        };

        db.Vendors.Add(vendor);
        db.VendorOfferings.Add(offering);
        await db.SaveChangesAsync();
        return (vendor, offering);
    }

    private static Booking MakeBooking(
        Vendor vendor,
        VendorOffering offering,
        BookingStatus status,
        decimal price,
        DateTime createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            QuotationId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            VendorId = vendor.Id,
            VendorServiceId = offering.Id,
            RequestedByUserId = Guid.NewGuid(),
            StartDateTime = createdAt.AddDays(1),
            EndDateTime = createdAt.AddDays(2),
            AgreedPrice = price,
            Status = status,
            CreatedAt = createdAt,
            CompletedAt = status == BookingStatus.COMPLETED ? createdAt.AddHours(1) : null,
            CancelledAt = status == BookingStatus.CANCELLED ? createdAt.AddHours(1) : null
        };

    [Fact]
    public async Task GetMyAnalyticsAsync_Throws_WhenVendorProfileMissing()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetMyAnalyticsAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetMyAnalyticsAsync_ComputesCountsRevenueAndQuotations()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        var (vendor, offering) = await SeedVendorAsync(db, userId);
        var now = DateTime.UtcNow;

        db.Bookings.AddRange(
            MakeBooking(vendor, offering, BookingStatus.COMPLETED, 10000m, now.AddDays(-5)),
            MakeBooking(vendor, offering, BookingStatus.COMPLETED, 15000m, now.AddDays(-4)),
            MakeBooking(vendor, offering, BookingStatus.CONFIRMED, 20000m, now.AddDays(-3)),
            MakeBooking(vendor, offering, BookingStatus.CANCELLED, 5000m, now.AddDays(-2)));

        db.Quotations.AddRange(
            new Quotation
            {
                Id = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                VendorId = vendor.Id,
                VendorServiceId = offering.Id,
                RequestedByUserId = Guid.NewGuid(),
                RequestedStartDateTime = now.AddDays(10),
                RequestedEndDateTime = now.AddDays(11),
                Status = QuotationStatus.REQUESTED,
                RequestedAt = now
            },
            new Quotation
            {
                Id = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                VendorId = vendor.Id,
                VendorServiceId = offering.Id,
                RequestedByUserId = Guid.NewGuid(),
                RequestedStartDateTime = now.AddDays(12),
                RequestedEndDateTime = now.AddDays(13),
                Status = QuotationStatus.ACCEPTED,
                RequestedAt = now,
                QuotedPrice = 12000m
            });

        db.VendorRatings.Add(new VendorRating
        {
            Id = Guid.NewGuid(),
            BookingId = db.Bookings.Local.First(b => b.Status == BookingStatus.COMPLETED).Id,
            VendorId = vendor.Id,
            ReviewerUserId = Guid.NewGuid(),
            Rating = 4,
            CreatedAt = now
        });
        db.VendorRatings.Add(new VendorRating
        {
            Id = Guid.NewGuid(),
            BookingId = db.Bookings.Local.Where(b => b.Status == BookingStatus.COMPLETED).Skip(1).First().Id,
            VendorId = vendor.Id,
            ReviewerUserId = Guid.NewGuid(),
            Rating = 5,
            CreatedAt = now
        });

        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetMyAnalyticsAsync(userId);

        Assert.Equal(4, result.TotalBookings);
        Assert.Equal(1, result.ConfirmedBookings);
        Assert.Equal(2, result.CompletedBookings);
        Assert.Equal(1, result.CancelledBookings);
        Assert.Equal(25000m, result.TotalRevenue);
        Assert.Equal(4.5m, result.AverageRating);
        Assert.Equal(2, result.ReviewCount);
        Assert.Equal(2, result.TotalQuotations);
        Assert.Equal(1, result.QuotationsAccepted);
        Assert.Equal(4, result.RecentBookings.Count);
        Assert.Equal("Wedding Package", result.RecentBookings[0].ServiceName);
    }

    [Fact]
    public async Task GetMyAnalyticsAsync_DoesNotIncludeOtherVendorData()
    {
        using var db = CreateDb();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var (vendorA, offeringA) = await SeedVendorAsync(db, userA, "Vendor A");
        var (vendorB, offeringB) = await SeedVendorAsync(db, userB, "Vendor B");
        var now = DateTime.UtcNow;

        db.Bookings.Add(MakeBooking(vendorA, offeringA, BookingStatus.COMPLETED, 1000m, now.AddDays(-1)));
        db.Bookings.Add(MakeBooking(vendorB, offeringB, BookingStatus.COMPLETED, 99999m, now));
        db.Quotations.Add(new Quotation
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            VendorId = vendorB.Id,
            VendorServiceId = offeringB.Id,
            RequestedByUserId = Guid.NewGuid(),
            RequestedStartDateTime = now.AddDays(2),
            RequestedEndDateTime = now.AddDays(3),
            Status = QuotationStatus.ACCEPTED,
            RequestedAt = now,
            QuotedPrice = 1m
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetMyAnalyticsAsync(userA);

        Assert.Equal(1, result.TotalBookings);
        Assert.Equal(1000m, result.TotalRevenue);
        Assert.Equal(0, result.TotalQuotations);
        Assert.Equal(0, result.QuotationsAccepted);
        Assert.DoesNotContain(result.RecentBookings, b => b.AgreedPrice == 99999m);
    }

    [Fact]
    public async Task GetMyAnalyticsAsync_LimitsRecentBookingsToFive()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        var (vendor, offering) = await SeedVendorAsync(db, userId);
        var now = DateTime.UtcNow;

        for (var i = 0; i < 7; i++)
        {
            db.Bookings.Add(MakeBooking(
                vendor,
                offering,
                BookingStatus.CONFIRMED,
                1000m + i,
                now.AddMinutes(-i)));
        }

        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetMyAnalyticsAsync(userId);

        Assert.Equal(7, result.TotalBookings);
        Assert.Equal(5, result.RecentBookings.Count);
        Assert.True(result.RecentBookings[0].CreatedAt >= result.RecentBookings[4].CreatedAt);
    }
}
