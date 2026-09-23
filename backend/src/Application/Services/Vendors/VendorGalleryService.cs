using Application.Common.Interfaces;
using Application.Dtos.Vendors;
using Domain.Entities;

namespace Application.Services.Vendors;

public class VendorGalleryService : IVendorGalleryService
{
    public const int MaxImagesPerVendor = 8;

    private readonly IVendorRepository _vendors;
    private readonly IVendorGalleryImageRepository _images;
    private readonly IVendorImageStorage _storage;

    public VendorGalleryService(
        IVendorRepository vendors,
        IVendorGalleryImageRepository images,
        IVendorImageStorage storage)
    {
        _vendors = vendors;
        _images = images;
        _storage = storage;
    }

    public async Task<IReadOnlyList<VendorGalleryImageResponse>> ListMineAsync(Guid userId)
    {
        var vendor = await RequireVendorAsync(userId);
        var items = await _images.ListByVendorIdAsync(vendor.Id);
        return items.Select(ToResponse).ToList();
    }

    public async Task<VendorGalleryImageResponse> UploadAsync(
        Guid userId,
        Stream content,
        string contentType,
        long contentLength)
    {
        var vendor = await RequireVendorAsync(userId);
        var count = await _images.CountByVendorIdAsync(vendor.Id);
        if (count >= MaxImagesPerVendor)
        {
            throw new InvalidOperationException($"You can upload up to {MaxImagesPerVendor} gallery images.");
        }

        var url = await _storage.SaveAsync(vendor.Id, content, contentType, contentLength);
        var image = new VendorGalleryImage
        {
            Id = Guid.NewGuid(),
            VendorId = vendor.Id,
            ImageUrl = url,
            CreatedAt = DateTime.UtcNow
        };

        await _images.AddAsync(image);
        await _images.SaveChangesAsync();
        return ToResponse(image);
    }

    public async Task DeleteAsync(Guid userId, Guid imageId)
    {
        var vendor = await RequireVendorAsync(userId);
        var image = await _images.GetByIdAsync(imageId)
            ?? throw new KeyNotFoundException("Gallery image not found.");

        if (image.VendorId != vendor.Id)
        {
            throw new UnauthorizedAccessException("You can only manage your own gallery images.");
        }

        _images.Remove(image);
        await _images.SaveChangesAsync();
        await _storage.DeleteIfExistsAsync(image.ImageUrl);
    }

    private async Task<Vendor> RequireVendorAsync(Guid userId) =>
        await _vendors.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Create a vendor profile before managing images.");

    private static VendorGalleryImageResponse ToResponse(VendorGalleryImage image) => new()
    {
        Id = image.Id,
        VendorId = image.VendorId,
        ImageUrl = image.ImageUrl,
        CreatedAt = image.CreatedAt
    };
}
