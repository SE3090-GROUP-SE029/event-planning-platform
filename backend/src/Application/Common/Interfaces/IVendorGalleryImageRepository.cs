using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IVendorGalleryImageRepository
{
    Task<IReadOnlyList<VendorGalleryImage>> ListByVendorIdAsync(Guid vendorId);
    Task<int> CountByVendorIdAsync(Guid vendorId);
    Task<VendorGalleryImage?> GetByIdAsync(Guid id);
    Task AddAsync(VendorGalleryImage image);
    void Remove(VendorGalleryImage image);
    Task SaveChangesAsync();
}
