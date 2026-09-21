namespace Application.Dtos.Vendors;

public class CreateVendorOfferingRequest
{
    public string ServiceName { get; set; } = default!;
    public string? Description { get; set; }
}
