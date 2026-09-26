using Application.Dtos.Vendors;
using Application.Services.Vendors;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Backend.UnitTests;

public class VendorAvailabilityServiceTests
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

    private static async Task SeedVendorAsync(AppDbContext db, Guid userId)
    {
        var vendorService = new VendorService(new VendorRepository(db), new FakeVendorImageStorage());
        await vendorService.CreateProfileAsync(userId, new CreateVendorProfileRequest
        {
            BusinessName = "KG Photography",
            Category = "PHOTOGRAPHY",
            ContactEmail = "kg@photo.test",
            ContactPhone = "0770001111",
            Address = "12 Studio Road",
            Description = "Event photography"
        });
    }

    private static VendorAvailabilityService CreateService(AppDbContext db) =>
        new(new VendorRepository(db), new VendorAvailabilityRepository(db));

    [Fact]
    public async Task CreateAsync_AddsAvailablePeriod()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedVendorAsync(db, userId);
        var service = CreateService(db);

        var created = await service.CreateAsync(userId, new CreateVendorAvailabilityRequest
        {
            StartDateTime = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc),
            EndDateTime = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc),
            IsAvailable = true
        });

        Assert.True(created.IsAvailable);
        Assert.Equal(new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc), created.StartDateTime);
        Assert.Equal(new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc), created.EndDateTime);
        Assert.Single(db.VendorAvailabilities);
    }

    [Fact]
    public async Task CreateAsync_AddsUnavailablePeriod()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedVendorAsync(db, userId);
        var service = CreateService(db);

        var created = await service.CreateAsync(userId, new CreateVendorAvailabilityRequest
        {
            StartDateTime = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc),
            EndDateTime = new DateTime(2026, 10, 16, 0, 0, 0, DateTimeKind.Utc),
            IsAvailable = false
        });

        Assert.False(created.IsAvailable);
    }

    [Fact]
    public async Task ListMineAsync_ReturnsOnlyOwnPeriodsOrderedByStart()
    {
        using var db = CreateDb();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await SeedVendorAsync(db, userA);
        await SeedVendorAsync(db, userB);
        var service = CreateService(db);

        await service.CreateAsync(userA, new CreateVendorAvailabilityRequest
        {
            StartDateTime = new DateTime(2026, 10, 12, 9, 0, 0, DateTimeKind.Utc),
            EndDateTime = new DateTime(2026, 10, 12, 22, 0, 0, DateTimeKind.Utc),
            IsAvailable = true
        });
        await service.CreateAsync(userA, new CreateVendorAvailabilityRequest
        {
            StartDateTime = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc),
            EndDateTime = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc),
            IsAvailable = true
        });
        await service.CreateAsync(userB, new CreateVendorAvailabilityRequest
        {
            StartDateTime = new DateTime(2026, 10, 11, 8, 0, 0, DateTimeKind.Utc),
            EndDateTime = new DateTime(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc),
            IsAvailable = true
        });

        var list = await service.ListMineAsync(userA);

        Assert.Equal(2, list.Count);
        Assert.Equal(new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc), list[0].StartDateTime);
        Assert.Equal(new DateTime(2026, 10, 12, 9, 0, 0, DateTimeKind.Utc), list[1].StartDateTime);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOwnedPeriod()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedVendorAsync(db, userId);
        var service = CreateService(db);
        var created = await service.CreateAsync(userId, new CreateVendorAvailabilityRequest
        {
            StartDateTime = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc),
            EndDateTime = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc),
            IsAvailable = true
        });

        var updated = await service.UpdateAsync(userId, created.Id, new UpdateVendorAvailabilityRequest
        {
            StartDateTime = new DateTime(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc),
            EndDateTime = new DateTime(2026, 10, 10, 17, 0, 0, DateTimeKind.Utc),
            IsAvailable = false
        });

        Assert.False(updated.IsAvailable);
        Assert.Equal(new DateTime(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc), updated.StartDateTime);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task DeleteAsync_RemovesOwnedPeriod()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedVendorAsync(db, userId);
        var service = CreateService(db);
        var created = await service.CreateAsync(userId, new CreateVendorAvailabilityRequest
        {
            StartDateTime = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc),
            EndDateTime = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc),
            IsAvailable = true
        });

        await service.DeleteAsync(userId, created.Id);

        Assert.Empty(db.VendorAvailabilities);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenStartNotBeforeEnd()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedVendorAsync(db, userId);
        var service = CreateService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(userId, new CreateVendorAvailabilityRequest
            {
                StartDateTime = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc),
                EndDateTime = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc),
                IsAvailable = true
            }));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenPeriodOverlaps()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedVendorAsync(db, userId);
        var service = CreateService(db);

        await service.CreateAsync(userId, new CreateVendorAvailabilityRequest
        {
            StartDateTime = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc),
            EndDateTime = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc),
            IsAvailable = true
        });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(userId, new CreateVendorAvailabilityRequest
            {
                StartDateTime = new DateTime(2026, 10, 10, 16, 0, 0, DateTimeKind.Utc),
                EndDateTime = new DateTime(2026, 10, 10, 20, 0, 0, DateTimeKind.Utc),
                IsAvailable = false
            }));
    }

    [Fact]
    public async Task CreateAsync_AllowsAdjacentPeriods()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedVendorAsync(db, userId);
        var service = CreateService(db);

        await service.CreateAsync(userId, new CreateVendorAvailabilityRequest
        {
            StartDateTime = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc),
            EndDateTime = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc),
            IsAvailable = true
        });

        var adjacent = await service.CreateAsync(userId, new CreateVendorAvailabilityRequest
        {
            StartDateTime = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc),
            EndDateTime = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc),
            IsAvailable = true
        });

        Assert.Equal(2, db.VendorAvailabilities.Count());
        Assert.Equal(new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc), adjacent.StartDateTime);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenPeriodBelongsToAnotherVendor()
    {
        using var db = CreateDb();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await SeedVendorAsync(db, userA);
        await SeedVendorAsync(db, userB);
        var service = CreateService(db);
        var created = await service.CreateAsync(userA, new CreateVendorAvailabilityRequest
        {
            StartDateTime = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc),
            EndDateTime = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc),
            IsAvailable = true
        });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateAsync(userB, created.Id, new UpdateVendorAvailabilityRequest
            {
                StartDateTime = new DateTime(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc),
                EndDateTime = new DateTime(2026, 10, 10, 17, 0, 0, DateTimeKind.Utc),
                IsAvailable = false
            }));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenVendorProfileMissing()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateAsync(Guid.NewGuid(), new CreateVendorAvailabilityRequest
            {
                StartDateTime = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc),
                EndDateTime = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc),
                IsAvailable = true
            }));
    }
}
