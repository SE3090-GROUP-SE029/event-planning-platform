namespace Application.Dtos.Vendors;

public class VendorAnalyticsBookingItemResponse
{
    public Guid Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public decimal AgreedPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool HasReview { get; set; }
}
