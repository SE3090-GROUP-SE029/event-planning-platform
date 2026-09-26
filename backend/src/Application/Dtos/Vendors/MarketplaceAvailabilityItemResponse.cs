namespace Application.Dtos.Vendors;

public class MarketplaceAvailabilityItemResponse
{
    public Guid Id { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public bool IsAvailable { get; set; }
}
