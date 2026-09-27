using Domain.Enums;

namespace Domain.Entities;

public class Booking
{
    public Guid Id { get; set; }
    public Guid QuotationId { get; set; }
    public Guid EventId { get; set; }
    public Guid VendorId { get; set; }
    public Guid VendorServiceId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public decimal AgreedPrice { get; set; }
    public string? VendorTerms { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.CONFIRMED;
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}
