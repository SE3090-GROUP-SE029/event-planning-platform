using Application.Common.Interfaces;
using Application.Dtos.Vendors;
using Application.Services.Vendors;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Backend.UnitTests;

public class VendorGalleryServiceTests
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
            BusinessName = "Studio",
            Category = "PHOTOGRAPHY",
            ContactEmail = "studio@test.com",
            ContactPhone = "0770000000",
            Address = "Colombo"
        });
    }

    [Fact]
    public async Task UploadAsync_AddsGalleryImage()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedVendorAsync(db, userId);
        var storage = new FakeVendorImageStorage();
        var service = new VendorGalleryService(
            new VendorRepository(db),
            new VendorGalleryImageRepository(db),
            storage);

        await using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var created = await service.UploadAsync(userId, stream, "image/jpeg", 4);

        Assert.Equal(storage.LastSavedUrl, created.ImageUrl);
        Assert.Single(db.VendorGalleryImages);
    }

    [Fact]
    public async Task DeleteAsync_RemovesOwnedImage()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedVendorAsync(db, userId);
        var service = new VendorGalleryService(
            new VendorRepository(db),
            new VendorGalleryImageRepository(db),
            new FakeVendorImageStorage());

        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var created = await service.UploadAsync(userId, stream, "image/png", 3);
        await service.DeleteAsync(userId, created.Id);

        Assert.Empty(db.VendorGalleryImages);
    }

    [Fact]
    public async Task UploadAsync_Throws_WhenLimitReached()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedVendorAsync(db, userId);
        var vendor = await db.Vendors.FirstAsync(v => v.UserId == userId);

        for (var i = 0; i < VendorGalleryService.MaxImagesPerVendor; i++)
        {
            db.VendorGalleryImages.Add(new VendorGalleryImage
            {
                Id = Guid.NewGuid(),
                VendorId = vendor.Id,
                ImageUrl = $"/uploads/vendors/{i}.jpg",
                CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();

        var service = new VendorGalleryService(
            new VendorRepository(db),
            new VendorGalleryImageRepository(db),
            new FakeVendorImageStorage());

        await using var stream = new MemoryStream(new byte[] { 9 });
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadAsync(userId, stream, "image/jpeg", 1));
    }
}
