namespace Application.Dtos.Vendors;

public class CreateVendorProfileRequest
{
    public string BusinessName { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string ContactEmail { get; set; } = default!;
    public string ContactPhone { get; set; } = default!;
    public string? Description { get; set; }
}
