namespace Application.Dtos.Vendors;

public class MarketplaceVendorDetailResponse
{
    public Guid Id { get; set; }
    public string BusinessName { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string? Description { get; set; }
    public string Address { get; set; } = default!;
    public string? ProfileImageUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public IReadOnlyList<MarketplaceGalleryImageResponse> Images { get; set; } = [];
    public IReadOnlyList<MarketplaceServiceItemResponse> Services { get; set; } = [];
    public IReadOnlyList<MarketplaceAvailabilityItemResponse> Availability { get; set; } = [];
}
