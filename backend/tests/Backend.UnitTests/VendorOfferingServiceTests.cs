using Application.Dtos.Vendors;
using Application.Services.Vendors;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Backend.UnitTests;

public class VendorOfferingServiceTests
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

    private static async Task<Guid> SeedVendorAsync(AppDbContext db, Guid userId)
    {
        var vendorService = new VendorService(new VendorRepository(db), new FakeVendorImageStorage());
        var profile = await vendorService.CreateProfileAsync(userId, new CreateVendorProfileRequest
        {
            BusinessName = "Lens & Light",
            Category = "PHOTOGRAPHY",
            ContactEmail = "studio@lens.test",
            ContactPhone = "0771112222",
            Address = "5 Studio Lane",
            Description = "Wedding photography"
        });
        return profile.Id;
    }

    [Fact]
    public async Task CreateAsync_AddsService_ForCurrentVendor()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        var vendorId = await SeedVendorAsync(db, userId);
        var service = new VendorOfferingService(new VendorRepository(db), new VendorOfferingRepository(db));

        var created = await service.CreateAsync(userId, new CreateVendorOfferingRequest
        {
            ServiceName = "Wedding Photography",
            Description = "Full-day wedding photography service"
        });

        Assert.Equal(vendorId, created.VendorId);
        Assert.Equal("Wedding Photography", created.ServiceName);
        Assert.Equal("Full-day wedding photography service", created.Description);
        Assert.Null(created.Price);
        Assert.Null(created.PricingType);
        Assert.Single(db.VendorOfferings);
    }

    [Fact]
    public async Task CreateAsync_SetsPriceAndPricingType()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedVendorAsync(db, userId);
        var service = new VendorOfferingService(new VendorRepository(db), new VendorOfferingRepository(db));

        var created = await service.CreateAsync(userId, new CreateVendorOfferingRequest
        {
            ServiceName = "Wedding Photography",
            Price = 85000.50m,
            PricingType = "FIXED"
        });

        Assert.Equal(85000.50m, created.Price);
        Assert.Equal("FIXED", created.PricingType);
    }

    [Fact]
    public async Task ListMineAsync_ReturnsOnlyOwnServices()
    {
        using var db = CreateDb();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await SeedVendorAsync(db, userA);
        await SeedVendorAsync(db, userB);
        var service = new VendorOfferingService(new VendorRepository(db), new VendorOfferingRepository(db));

        await service.CreateAsync(userA, new CreateVendorOfferingRequest { ServiceName = "A Service" });
        await service.CreateAsync(userB, new CreateVendorOfferingRequest { ServiceName = "B Service" });

        var list = await service.ListMineAsync(userA);

        Assert.Single(list);
        Assert.Equal("A Service", list[0].ServiceName);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOwnedService()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedVendorAsync(db, userId);
        var service = new VendorOfferingService(new VendorRepository(db), new VendorOfferingRepository(db));
        var created = await service.CreateAsync(userId, new CreateVendorOfferingRequest
        {
            ServiceName = "Old Name",
            Description = "Old"
        });

        var updated = await service.UpdateAsync(userId, created.Id, new UpdateVendorOfferingRequest
        {
            ServiceName = "New Name",
            Description = "Updated"
        });

        Assert.Equal("New Name", updated.ServiceName);
        Assert.Equal("Updated", updated.Description);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesPriceAndPricingType()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedVendorAsync(db, userId);
        var service = new VendorOfferingService(new VendorRepository(db), new VendorOfferingRepository(db));
        var created = await service.CreateAsync(userId, new CreateVendorOfferingRequest
        {
            ServiceName = "Catering",
            Price = 1000m,
            PricingType = "FIXED"
        });

        var updated = await service.UpdateAsync(userId, created.Id, new UpdateVendorOfferingRequest
        {
            ServiceName = "Catering",
            Price = 2500.75m,
            PricingType = "PER_PERSON"
        });

        Assert.Equal(2500.75m, updated.Price);
        Assert.Equal("PER_PERSON", updated.PricingType);
    }

    [Fact]
    public async Task UpdateAsync_ClearsPriceAndPricingType()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedVendorAsync(db, userId);
        var service = new VendorOfferingService(new VendorRepository(db), new VendorOfferingRepository(db));
        var created = await service.CreateAsync(userId, new CreateVendorOfferingRequest
        {
            ServiceName = "DJ Package",
            Price = 40000m,
            PricingType = "PER_DAY"
        });

        var updated = await service.UpdateAsync(userId, created.Id, new UpdateVendorOfferingRequest
        {
            ServiceName = "DJ Package",
            Price = null,
            PricingType = null
        });

        Assert.Null(updated.Price);
        Assert.Null(updated.PricingType);
    }

    [Fact]
    public async Task DeleteAsync_RemovesOwnedService()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedVendorAsync(db, userId);
        var service = new VendorOfferingService(new VendorRepository(db), new VendorOfferingRepository(db));
        var created = await service.CreateAsync(userId, new CreateVendorOfferingRequest
        {
            ServiceName = "To Delete"
        });

        await service.DeleteAsync(userId, created.Id);

        Assert.Empty(db.VendorOfferings);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenServiceBelongsToAnotherVendor()
    {
        using var db = CreateDb();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await SeedVendorAsync(db, userA);
        await SeedVendorAsync(db, userB);
        var service = new VendorOfferingService(new VendorRepository(db), new VendorOfferingRepository(db));
        var created = await service.CreateAsync(userA, new CreateVendorOfferingRequest
        {
            ServiceName = "A Service",
            Price = 10000m,
            PricingType = "FIXED"
        });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateAsync(userB, created.Id, new UpdateVendorOfferingRequest
            {
                ServiceName = "Hacked",
                Price = 1m,
                PricingType = "FIXED"
            }));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenVendorProfileMissing()
    {
        using var db = CreateDb();
        var service = new VendorOfferingService(new VendorRepository(db), new VendorOfferingRepository(db));

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateAsync(Guid.NewGuid(), new CreateVendorOfferingRequest
            {
                ServiceName = "Orphan Service"
            }));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenPriceSetWithoutPricingType()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedVendorAsync(db, userId);
        var service = new VendorOfferingService(new VendorRepository(db), new VendorOfferingRepository(db));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(userId, new CreateVendorOfferingRequest
            {
                ServiceName = "Bad Pricing",
                Price = 1000m
            }));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenPricingTypeInvalid()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedVendorAsync(db, userId);
        var service = new VendorOfferingService(new VendorRepository(db), new VendorOfferingRepository(db));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(userId, new CreateVendorOfferingRequest
            {
                ServiceName = "Bad Type",
                Price = 1000m,
                PricingType = "WEEKLY"
            }));
    }
}
