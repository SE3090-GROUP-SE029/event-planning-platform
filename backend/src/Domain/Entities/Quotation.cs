using Domain.Enums;

namespace Domain.Entities;

public class Quotation
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid VendorId { get; set; }
    public Guid VendorServiceId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public DateTime RequestedStartDateTime { get; set; }
    public DateTime RequestedEndDateTime { get; set; }
    public string? CustomerMessage { get; set; }
    public decimal? QuotedPrice { get; set; }
    public string? VendorTerms { get; set; }
    public QuotationStatus Status { get; set; } = QuotationStatus.REQUESTED;
    public DateTime RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
