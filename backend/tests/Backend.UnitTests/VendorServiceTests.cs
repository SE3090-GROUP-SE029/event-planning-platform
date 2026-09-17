using Application.Dtos.Vendors;
using Application.Services.Vendors;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Backend.UnitTests;

public class VendorServiceTests
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

    private static CreateVendorProfileRequest ValidCreateRequest() => new()
    {
        BusinessName = "Green Leaf Catering",
        Category = "CATERING",
        ContactEmail = "hello@greenleaf.test",
        ContactPhone = "0771234567",
        Description = "Vegetarian catering for events"
    };

    [Fact]
    public async Task CreateProfileAsync_CreatesPendingVendor_WhenRequestIsValid()
    {
        using var db = CreateDb();
        var service = new VendorService(new VendorRepository(db));
        var userId = Guid.NewGuid();

        var result = await service.CreateProfileAsync(userId, ValidCreateRequest());

        Assert.Equal(userId, result.UserId);
        Assert.Equal("Green Leaf Catering", result.BusinessName);
        Assert.Equal("CATERING", result.Category);
        Assert.Equal("hello@greenleaf.test", result.ContactEmail);
        Assert.Equal(nameof(VendorStatus.PENDING), result.Status);
        Assert.Single(db.Vendors);
    }

    [Fact]
    public async Task CreateProfileAsync_Throws_WhenProfileAlreadyExists()
    {
        using var db = CreateDb();
        var service = new VendorService(new VendorRepository(db));
        var userId = Guid.NewGuid();
        await service.CreateProfileAsync(userId, ValidCreateRequest());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateProfileAsync(userId, ValidCreateRequest()));

        Assert.Contains("already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateProfileAsync_Throws_WhenCategoryIsInvalid()
    {
        using var db = CreateDb();
        var service = new VendorService(new VendorRepository(db));
        var request = ValidCreateRequest();
        request.Category = "NOT_A_CATEGORY";

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateProfileAsync(Guid.NewGuid(), request));
    }

    [Fact]
    public async Task GetMyProfileAsync_Throws_WhenProfileDoesNotExist()
    {
        using var db = CreateDb();
        var service = new VendorService(new VendorRepository(db));

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetMyProfileAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateMyProfileAsync_UpdatesBusinessFields_WithoutChangingStatus()
    {
        using var db = CreateDb();
        var service = new VendorService(new VendorRepository(db));
        var userId = Guid.NewGuid();
        await service.CreateProfileAsync(userId, ValidCreateRequest());

        var updated = await service.UpdateMyProfileAsync(userId, new UpdateVendorProfileRequest
        {
            BusinessName = "Green Leaf Events",
            Category = "FLORIST",
            ContactEmail = "events@greenleaf.test",
            ContactPhone = "0779998888",
            Description = "Updated description"
        });

        Assert.Equal("Green Leaf Events", updated.BusinessName);
        Assert.Equal("FLORIST", updated.Category);
        Assert.Equal("events@greenleaf.test", updated.ContactEmail);
        Assert.Equal("Updated description", updated.Description);
        Assert.Equal(nameof(VendorStatus.PENDING), updated.Status);
        Assert.NotNull(updated.UpdatedAt);
    }
}
