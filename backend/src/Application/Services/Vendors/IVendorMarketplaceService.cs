using Application.Dtos.Vendors;

namespace Application.Services.Vendors;

public interface IVendorMarketplaceService
{
    Task<MarketplaceVendorListResponse> ListAsync(VendorMarketplaceQuery query);
    Task<MarketplaceVendorDetailResponse> GetByIdAsync(Guid vendorId);
}
