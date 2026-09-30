namespace Application.Dtos.Vendors;

public class MarketplaceVendorListItemResponse
{
    public Guid Id { get; set; }
    public string BusinessName { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string? ShortDescription { get; set; }
    public string Address { get; set; } = default!;
    public string? ProfileImageUrl { get; set; }
    public decimal? StartingPrice { get; set; }
    public string? StartingPricingType { get; set; }
    public decimal? AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
