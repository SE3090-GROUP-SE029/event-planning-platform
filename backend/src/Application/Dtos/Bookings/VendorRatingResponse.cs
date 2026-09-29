namespace Application.Dtos.Bookings;

public class VendorRatingResponse
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid VendorId { get; set; }
    public Guid ReviewerUserId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
