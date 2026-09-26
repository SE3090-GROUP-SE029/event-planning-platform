namespace Application.Dtos.Vendors;

public class MarketplaceVendorListResponse
{
    public IReadOnlyList<MarketplaceVendorListItemResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
