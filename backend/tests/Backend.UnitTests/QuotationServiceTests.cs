using Application.Dtos.Quotations;
using Application.Services.Quotations;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Backend.UnitTests;

public class QuotationServiceTests
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

    private static QuotationService CreateService(AppDbContext db) =>
        new(
            new QuotationRepository(db),
            new VendorRepository(db),
            new VendorOfferingRepository(db),
            new VendorAvailabilityRepository(db),
            new EventRepository(db));

    private static async Task<(Vendor Vendor, VendorOffering Offering, Event Event)> SeedHappyPathAsync(
        AppDbContext db,
        Guid plannerUserId,
        VendorStatus vendorStatus = VendorStatus.APPROVED)
    {
        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BusinessName = "KG Photography",
            Category = BusinessCategory.PHOTOGRAPHY,
            ContactEmail = "kg@photo.test",
            ContactPhone = "0770001111",
            Address = "12 Studio Road",
            Status = vendorStatus,
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

        db.Vendors.Add(vendor);
        db.VendorOfferings.Add(offering);
        db.Events.Add(eventEntity);
        db.VendorAvailabilities.Add(availability);
        await db.SaveChangesAsync();
        return (vendor, offering, eventEntity);
    }

    [Fact]
    public async Task CreateAsync_CreatesRequestedQuotation()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (vendor, offering, eventEntity) = await SeedHappyPathAsync(db, plannerId);
        var service = CreateService(db);

        var result = await service.CreateAsync(plannerId, new CreateQuotationRequest
        {
            VendorId = vendor.Id,
            VendorServiceId = offering.Id,
            EventId = eventEntity.Id,
            RequestedStartDateTime = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
            RequestedEndDateTime = new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc),
            CustomerMessage = "Need outdoor setup"
        });

        Assert.Equal(nameof(QuotationStatus.REQUESTED), result.Status);
        Assert.Equal(vendor.BusinessName, result.VendorBusinessName);
        Assert.Equal(offering.ServiceName, result.ServiceName);
        Assert.Equal("Need outdoor setup", result.CustomerMessage);
        Assert.Null(result.QuotedPrice);
        Assert.Single(db.Quotations);
    }

    [Fact]
    public async Task CreateAsync_RejectsWhenEventNotOwned()
    {
        using var db = CreateDb();
        var ownerId = Guid.NewGuid();
        var (vendor, offering, eventEntity) = await SeedHappyPathAsync(db, ownerId);
        var service = CreateService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateAsync(Guid.NewGuid(), new CreateQuotationRequest
            {
                VendorId = vendor.Id,
                VendorServiceId = offering.Id,
                EventId = eventEntity.Id,
                RequestedStartDateTime = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
                RequestedEndDateTime = new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc)
            }));
    }

    [Fact]
    public async Task CreateAsync_RejectsUnapprovedVendor()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (vendor, offering, eventEntity) = await SeedHappyPathAsync(db, plannerId, VendorStatus.PENDING);
        var service = CreateService(db);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(plannerId, new CreateQuotationRequest
            {
                VendorId = vendor.Id,
                VendorServiceId = offering.Id,
                EventId = eventEntity.Id,
                RequestedStartDateTime = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
                RequestedEndDateTime = new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc)
            }));

        Assert.Contains("approved", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_RejectsServiceFromOtherVendor()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (vendor, _, eventEntity) = await SeedHappyPathAsync(db, plannerId);
        var otherOffering = new VendorOffering
        {
            Id = Guid.NewGuid(),
            VendorId = Guid.NewGuid(),
            ServiceName = "Other",
            CreatedAt = DateTime.UtcNow
        };
        db.VendorOfferings.Add(otherOffering);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(plannerId, new CreateQuotationRequest
            {
                VendorId = vendor.Id,
                VendorServiceId = otherOffering.Id,
                EventId = eventEntity.Id,
                RequestedStartDateTime = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
                RequestedEndDateTime = new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc)
            }));
    }

    [Fact]
    public async Task CreateAsync_RejectsInvalidDateRange()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (vendor, offering, eventEntity) = await SeedHappyPathAsync(db, plannerId);
        var service = CreateService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(plannerId, new CreateQuotationRequest
            {
                VendorId = vendor.Id,
                VendorServiceId = offering.Id,
                EventId = eventEntity.Id,
                RequestedStartDateTime = new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc),
                RequestedEndDateTime = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc)
            }));
    }

    [Fact]
    public async Task CreateAsync_RejectsWhenOutsideAvailableWindow()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (vendor, offering, eventEntity) = await SeedHappyPathAsync(db, plannerId);
        var service = CreateService(db);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(plannerId, new CreateQuotationRequest
            {
                VendorId = vendor.Id,
                VendorServiceId = offering.Id,
                EventId = eventEntity.Id,
                RequestedStartDateTime = new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc),
                RequestedEndDateTime = new DateTime(2026, 11, 2, 14, 0, 0, DateTimeKind.Utc)
            }));

        Assert.Contains("available", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RespondAsync_SetsQuotedStatusAndPrice()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (vendor, offering, eventEntity) = await SeedHappyPathAsync(db, plannerId);
        var service = CreateService(db);

        var created = await service.CreateAsync(plannerId, new CreateQuotationRequest
        {
            VendorId = vendor.Id,
            VendorServiceId = offering.Id,
            EventId = eventEntity.Id,
            RequestedStartDateTime = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
            RequestedEndDateTime = new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc)
        });

        var responded = await service.RespondAsync(vendor.UserId, created.Id, new RespondToQuotationRequest
        {
            QuotedPrice = 75000.50m,
            VendorTerms = "50% deposit required"
        });

        Assert.Equal(nameof(QuotationStatus.QUOTED), responded.Status);
        Assert.Equal(75000.50m, responded.QuotedPrice);
        Assert.Equal("50% deposit required", responded.VendorTerms);
        Assert.NotNull(responded.RespondedAt);
    }

    [Fact]
    public async Task RespondAsync_RejectsNegativePrice()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (vendor, offering, eventEntity) = await SeedHappyPathAsync(db, plannerId);
        var service = CreateService(db);

        var created = await service.CreateAsync(plannerId, new CreateQuotationRequest
        {
            VendorId = vendor.Id,
            VendorServiceId = offering.Id,
            EventId = eventEntity.Id,
            RequestedStartDateTime = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
            RequestedEndDateTime = new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc)
        });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RespondAsync(vendor.UserId, created.Id, new RespondToQuotationRequest
            {
                QuotedPrice = -1m
            }));
    }

    [Fact]
    public async Task RespondAsync_RejectsOtherVendor()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (vendor, offering, eventEntity) = await SeedHappyPathAsync(db, plannerId);
        var otherVendor = new Vendor
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BusinessName = "Other",
            Category = BusinessCategory.CATERING,
            ContactEmail = "o@test",
            ContactPhone = "1",
            Address = "A",
            Status = VendorStatus.APPROVED,
            CreatedAt = DateTime.UtcNow
        };
        db.Vendors.Add(otherVendor);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var created = await service.CreateAsync(plannerId, new CreateQuotationRequest
        {
            VendorId = vendor.Id,
            VendorServiceId = offering.Id,
            EventId = eventEntity.Id,
            RequestedStartDateTime = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
            RequestedEndDateTime = new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc)
        });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.RespondAsync(otherVendor.UserId, created.Id, new RespondToQuotationRequest
            {
                QuotedPrice = 100m
            }));
    }

    [Fact]
    public async Task ListMineAsync_ReturnsOnlyPlannerQuotations()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var otherPlanner = Guid.NewGuid();
        var (vendor, offering, eventEntity) = await SeedHappyPathAsync(db, plannerId);
        var otherEvent = new Event
        {
            Id = Guid.NewGuid(),
            OwnerId = otherPlanner,
            EventType = EventType.BIRTHDAY,
            GuestCount = 20,
            Budget = 10000m,
            PreferredVenue = "Home",
            PreferredDate = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            EventDuration = TimeSpan.FromHours(3),
            Status = EventStatus.DRAFT,
            CreatedAt = DateTime.UtcNow
        };
        db.Events.Add(otherEvent);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.CreateAsync(plannerId, new CreateQuotationRequest
        {
            VendorId = vendor.Id,
            VendorServiceId = offering.Id,
            EventId = eventEntity.Id,
            RequestedStartDateTime = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
            RequestedEndDateTime = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc)
        });
        await service.CreateAsync(otherPlanner, new CreateQuotationRequest
        {
            VendorId = vendor.Id,
            VendorServiceId = offering.Id,
            EventId = otherEvent.Id,
            RequestedStartDateTime = new DateTime(2026, 11, 1, 13, 0, 0, DateTimeKind.Utc),
            RequestedEndDateTime = new DateTime(2026, 11, 1, 15, 0, 0, DateTimeKind.Utc)
        });

        var mine = await service.ListMineAsync(plannerId);

        Assert.Single(mine);
        Assert.Equal(plannerId, mine[0].RequestedByUserId);
    }

    [Fact]
    public async Task GetByIdAsync_AllowsVendorOwner()
    {
        using var db = CreateDb();
        var plannerId = Guid.NewGuid();
        var (vendor, offering, eventEntity) = await SeedHappyPathAsync(db, plannerId);
        var service = CreateService(db);

        var created = await service.CreateAsync(plannerId, new CreateQuotationRequest
        {
            VendorId = vendor.Id,
            VendorServiceId = offering.Id,
            EventId = eventEntity.Id,
            RequestedStartDateTime = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
            RequestedEndDateTime = new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc)
        });

        var viewed = await service.GetByIdAsync(created.Id, vendor.UserId, isVendor: true);

        Assert.Equal(created.Id, viewed.Id);
        Assert.Equal(eventEntity.GuestCount, viewed.GuestCount);
    }
}
