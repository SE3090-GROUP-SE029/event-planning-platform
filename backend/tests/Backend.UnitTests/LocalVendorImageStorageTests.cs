using Infrastructure.Storage;
using Application.Dtos.Vendors;
using Application.Services.Vendors;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Backend.UnitTests;

public class LocalVendorImageStorageTests
{
    [Fact]
    public async Task SaveAsync_PersistsImageWithPublicRelativeUrl()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new LocalVendorImageStorage(root);
            await using var image = new MemoryStream(_validPngBytes);

            var url = await storage.SaveAsync(Guid.NewGuid(), image, "image/png", image.Length);

            Assert.StartsWith("/uploads/vendors/", url);
            Assert.True(File.Exists(ToDiskPath(root, url)));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SaveAsync_RejectsContentTypeThatDoesNotMatchImageSignature()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new LocalVendorImageStorage(root);
            await using var image = new MemoryStream(new byte[] { 1, 2, 3, 4 });

            var error = await Assert.ThrowsAsync<ArgumentException>(() =>
                storage.SaveAsync(Guid.NewGuid(), image, "image/jpeg", image.Length));

            Assert.Contains("not a valid", error.Message);
            Assert.False(Directory.Exists(root));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SaveAsync_RejectsImagesAboveSizeLimit()
    {
        var storage = new LocalVendorImageStorage(Path.GetTempPath());
        await using var image = new MemoryStream(
            new byte[(int)LocalVendorImageStorage.MaxFileBytes + 1]);

        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            storage.SaveAsync(Guid.NewGuid(), image, "image/png", image.Length));

        Assert.Contains("2 MB", error.Message);
    }

    [Fact]
    public async Task VendorService_PersistsImageUrlAndReplacesPreviousImage()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        try
        {
            using var db = new AppDbContext(options);
            db.Database.EnsureCreated();
            var storage = new LocalVendorImageStorage(root);
            var service = new VendorService(new VendorRepository(db), storage);
            var userId = Guid.NewGuid();
            await service.CreateProfileAsync(userId, new CreateVendorProfileRequest
            {
                BusinessName = "Test Vendor",
                Category = "PHOTOGRAPHY",
                ContactEmail = "vendor@example.test",
                ContactPhone = "0770000000",
                Address = "Colombo"
            });

            await using var firstUpload = new MemoryStream(_validPngBytes);
            var first = await service.UpdateProfileImageAsync(
                userId, firstUpload, "image/png", firstUpload.Length);
            var firstPath = ToDiskPath(root, first.ProfileImageUrl!);
            Assert.True(File.Exists(firstPath));

            await using var replacement = new MemoryStream(_validPngBytes);
            var updated = await service.UpdateProfileImageAsync(
                userId, replacement, "image/png", replacement.Length);

            Assert.NotEqual(first.ProfileImageUrl, updated.ProfileImageUrl);
            Assert.False(File.Exists(firstPath));
            Assert.True(File.Exists(ToDiskPath(root, updated.ProfileImageUrl!)));
            Assert.Equal(updated.ProfileImageUrl, (await service.GetMyProfileAsync(userId)).ProfileImageUrl);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static string ToDiskPath(string root, string url) =>
        Path.Combine(root, Path.GetFileName(url));

    private static readonly byte[] _validPngBytes =
        Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p8sAAAAASUVORK5CYII=");
}
