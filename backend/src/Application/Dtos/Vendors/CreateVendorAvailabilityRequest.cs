namespace Application.Dtos.Vendors;

public class CreateVendorAvailabilityRequest
{
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public bool IsAvailable { get; set; } = true;
}
