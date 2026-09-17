namespace Application.Dtos.Vendors;

public class VendorProfileResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string BusinessName { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string ContactEmail { get; set; } = default!;
    public string ContactPhone { get; set; } = default!;
    public string? Description { get; set; }
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
