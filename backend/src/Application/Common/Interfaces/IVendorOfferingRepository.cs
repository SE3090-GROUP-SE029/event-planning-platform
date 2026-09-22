using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IVendorOfferingRepository
{
    Task<IReadOnlyList<VendorOffering>> ListByVendorIdAsync(Guid vendorId);
    Task<VendorOffering?> GetByIdAsync(Guid id);
    Task AddAsync(VendorOffering offering);
    void Remove(VendorOffering offering);
    Task SaveChangesAsync();
}
