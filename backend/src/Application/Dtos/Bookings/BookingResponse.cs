namespace Application.Dtos.Bookings;

public class BookingResponse
{
    public Guid Id { get; set; }
    public Guid QuotationId { get; set; }
    public Guid EventId { get; set; }
    public string? EventType { get; set; }
    public int? GuestCount { get; set; }
    public DateTime? EventPreferredDate { get; set; }
    public Guid VendorId { get; set; }
    public string VendorBusinessName { get; set; } = string.Empty;
    public Guid VendorServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public Guid RequestedByUserId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public decimal AgreedPrice { get; set; }
    public string? VendorTerms { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public bool HasReview { get; set; }
}
