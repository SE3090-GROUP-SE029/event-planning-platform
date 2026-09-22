namespace Application.Dtos.Vendors;

public class VendorOfferingResponse
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string ServiceName { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
