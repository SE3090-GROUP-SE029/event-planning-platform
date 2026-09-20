using Domain.Enums;

namespace Application.Dtos.Events;

public class EventResponse
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public EventType EventType { get; set; }
    public int GuestCount { get; set; }
    public decimal Budget { get; set; }
    public string PreferredVenue { get; set; } = default!;
    public DateTime PreferredDate { get; set; }
    public TimeSpan EventDuration { get; set; }
    public string? Requirements { get; set; }
    public EventStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
