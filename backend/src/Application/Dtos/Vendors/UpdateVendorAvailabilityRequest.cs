namespace Application.Dtos.Vendors;

public class UpdateVendorAvailabilityRequest
{
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public bool IsAvailable { get; set; }
}
