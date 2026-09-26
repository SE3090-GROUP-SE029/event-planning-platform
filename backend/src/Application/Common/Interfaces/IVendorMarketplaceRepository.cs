using Application.Dtos.Vendors;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Interfaces;

public interface IVendorMarketplaceRepository
{
    Task<(IReadOnlyList<MarketplaceVendorListItemResponse> Items, int TotalCount)> SearchApprovedAsync(
        VendorMarketplaceQuery query,
        BusinessCategory? categoryFilter);

    Task<Vendor?> GetApprovedByIdAsync(Guid vendorId);
}
