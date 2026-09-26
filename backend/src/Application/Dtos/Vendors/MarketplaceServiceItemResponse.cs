namespace Application.Dtos.Vendors;

public class MarketplaceServiceItemResponse
{
    public Guid Id { get; set; }
    public string ServiceName { get; set; } = default!;
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? PricingType { get; set; }
}
