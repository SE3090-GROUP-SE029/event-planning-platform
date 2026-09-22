using Application.Dtos.Vendors;

namespace Application.Services.Vendors;

public interface IVendorOfferingService
{
    Task<VendorOfferingResponse> CreateAsync(Guid userId, CreateVendorOfferingRequest request);
    Task<IReadOnlyList<VendorOfferingResponse>> ListMineAsync(Guid userId);
    Task<VendorOfferingResponse> UpdateAsync(Guid userId, Guid offeringId, UpdateVendorOfferingRequest request);
    Task DeleteAsync(Guid userId, Guid offeringId);
}
