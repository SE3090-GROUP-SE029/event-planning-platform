using Application.Dtos.Vendors;

namespace Application.Services.Vendors;

public interface IVendorGalleryService
{
    Task<IReadOnlyList<VendorGalleryImageResponse>> ListMineAsync(Guid userId);
    Task<VendorGalleryImageResponse> UploadAsync(
        Guid userId,
        Stream content,
        string contentType,
        long contentLength);
    Task DeleteAsync(Guid userId, Guid imageId);
}
