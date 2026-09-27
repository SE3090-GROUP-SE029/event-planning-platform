namespace Domain.Entities;

public class VendorAvailability
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public bool IsAvailable { get; set; }
    /// <summary>
    /// Set when this unavailable segment was created as a booking lock.
    /// Null for vendor-managed availability periods.
    /// </summary>
    public Guid? SourceBookingId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
