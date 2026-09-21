namespace Domain.Entities;

public class VendorGalleryImage
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string ImageUrl { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
