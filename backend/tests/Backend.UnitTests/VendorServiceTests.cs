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
        Address = "12 Flower Road, Colombo",
        Description = "Vegetarian catering for events"
    };

    [Fact]
    public async Task CreateProfileAsync_CreatesPendingVendor_WhenRequestIsValid()
    {
        using var db = CreateDb();
        var service = new VendorService(new VendorRepository(db), new FakeVendorImageStorage());
        var userId = Guid.NewGuid();

        var result = await service.CreateProfileAsync(userId, ValidCreateRequest());

        Assert.Equal(userId, result.UserId);
        Assert.Equal("Green Leaf Catering", result.BusinessName);
        Assert.Equal("CATERING", result.Category);
        Assert.Equal("hello@greenleaf.test", result.ContactEmail);
        Assert.Equal("12 Flower Road, Colombo", result.Address);
        Assert.Equal(nameof(VendorStatus.PENDING), result.Status);
        Assert.Single(db.Vendors);
    }

    [Fact]
    public async Task CreateProfileAsync_Throws_WhenProfileAlreadyExists()
    {
        using var db = CreateDb();
        var service = new VendorService(new VendorRepository(db), new FakeVendorImageStorage());
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
        var service = new VendorService(new VendorRepository(db), new FakeVendorImageStorage());
        var request = ValidCreateRequest();
        request.Category = "NOT_A_CATEGORY";

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateProfileAsync(Guid.NewGuid(), request));
    }

    [Fact]
    public async Task CreateProfileAsync_Throws_WhenAddressIsMissing()
    {
        using var db = CreateDb();
        var service = new VendorService(new VendorRepository(db), new FakeVendorImageStorage());
        var request = ValidCreateRequest();
        request.Address = "   ";

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateProfileAsync(Guid.NewGuid(), request));
    }

    [Fact]
    public async Task GetMyProfileAsync_Throws_WhenProfileDoesNotExist()
    {
        using var db = CreateDb();
        var service = new VendorService(new VendorRepository(db), new FakeVendorImageStorage());

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetMyProfileAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateMyProfileAsync_UpdatesBusinessFields_WithoutChangingStatus()
    {
        using var db = CreateDb();
        var service = new VendorService(new VendorRepository(db), new FakeVendorImageStorage());
        var userId = Guid.NewGuid();
        await service.CreateProfileAsync(userId, ValidCreateRequest());

        var updated = await service.UpdateMyProfileAsync(userId, new UpdateVendorProfileRequest
        {
            BusinessName = "Green Leaf Events",
            Category = "FLORIST",
            ContactEmail = "events@greenleaf.test",
            ContactPhone = "0779998888",
            Address = "88 Lake Drive, Kandy",
            Description = "Updated description"
        });

        Assert.Equal("Green Leaf Events", updated.BusinessName);
        Assert.Equal("FLORIST", updated.Category);
        Assert.Equal("events@greenleaf.test", updated.ContactEmail);
        Assert.Equal("88 Lake Drive, Kandy", updated.Address);
        Assert.Equal("Updated description", updated.Description);
        Assert.Equal(nameof(VendorStatus.PENDING), updated.Status);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateProfileImageAsync_SetsProfileImageUrl()
    {
        using var db = CreateDb();
        var images = new FakeVendorImageStorage();
        var service = new VendorService(new VendorRepository(db), images);
        var userId = Guid.NewGuid();
        await service.CreateProfileAsync(userId, ValidCreateRequest());

        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var updated = await service.UpdateProfileImageAsync(userId, stream, "image/jpeg", 3);

        Assert.Equal(images.LastSavedUrl, updated.ProfileImageUrl);
        Assert.StartsWith("/uploads/vendors/", updated.ProfileImageUrl);
    }

    [Fact]
    public async Task CreateProfileAsync_SavesWebsiteUrl_WhenValid()
    {
        using var db = CreateDb();
        var service = new VendorService(new VendorRepository(db), new FakeVendorImageStorage());
        var request = ValidCreateRequest();
        request.WebsiteUrl = "https://greenleaf.test";

        var result = await service.CreateProfileAsync(Guid.NewGuid(), request);

        Assert.Equal("https://greenleaf.test", result.WebsiteUrl);
    }
}
