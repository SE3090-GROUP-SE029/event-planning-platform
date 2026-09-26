namespace Application.Dtos.Quotations;

public class QuotationResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string? EventType { get; set; }
    public int? GuestCount { get; set; }
    public DateTime? EventPreferredDate { get; set; }
    public string? EventRequirements { get; set; }
    public Guid VendorId { get; set; }
    public string VendorBusinessName { get; set; } = default!;
    public Guid VendorServiceId { get; set; }
    public string ServiceName { get; set; } = default!;
    public Guid RequestedByUserId { get; set; }
    public DateTime RequestedStartDateTime { get; set; }
    public DateTime RequestedEndDateTime { get; set; }
    public string? CustomerMessage { get; set; }
    public decimal? QuotedPrice { get; set; }
    public string? VendorTerms { get; set; }
    public string Status { get; set; } = default!;
    public DateTime RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
