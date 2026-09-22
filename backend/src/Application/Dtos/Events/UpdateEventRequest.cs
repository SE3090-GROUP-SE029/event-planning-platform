using Domain.Enums;

namespace Application.Dtos.Events;

public class UpdateEventRequest
{
    public EventType? EventType { get; set; }
    public int GuestCount { get; set; }
    public decimal Budget { get; set; }
    public string PreferredVenue { get; set; } = default!;
    public DateTime PreferredDate { get; set; }
    public string? Requirements { get; set; }
    public EventStatus Status { get; set; }
}
