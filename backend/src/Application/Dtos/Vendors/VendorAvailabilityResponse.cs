namespace Application.Dtos.Vendors;

public class VendorAvailabilityResponse
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
