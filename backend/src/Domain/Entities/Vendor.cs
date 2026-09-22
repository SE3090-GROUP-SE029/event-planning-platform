using Domain.Enums;

namespace Domain.Entities;

public class Vendor
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string BusinessName { get; set; } = default!;
    public BusinessCategory Category { get; set; }
    public string ContactEmail { get; set; } = default!;
    public string ContactPhone { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string? Description { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public VendorStatus Status { get; set; } = VendorStatus.PENDING;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
