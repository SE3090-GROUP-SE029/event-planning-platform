namespace Application.Dtos.Vendors;

public class UpdateVendorOfferingRequest
{
    public string ServiceName { get; set; } = default!;
    public string? Description { get; set; }
}
