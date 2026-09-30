namespace Application.Dtos.Vendors;

public class VendorAnalyticsResponse
{
    public int TotalBookings { get; set; }
    public int ConfirmedBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal? AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int TotalQuotations { get; set; }
    public int QuotationsAccepted { get; set; }
    public IReadOnlyList<VendorAnalyticsBookingItemResponse> RecentBookings { get; set; } = [];
}
