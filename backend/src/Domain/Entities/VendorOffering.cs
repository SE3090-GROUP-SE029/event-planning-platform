namespace Domain.Entities;

/// <summary>
/// A service offered by a vendor. Maps to the VendorServices table.
/// Named VendorOffering to avoid clashing with Application.Services.Vendors.VendorService.
/// </summary>
public class VendorOffering
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string ServiceName { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
