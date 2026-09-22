using Domain.Enums;

namespace Domain.Entities;

public class Event
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public EventType EventType { get; set; }
    public int GuestCount { get; set; }
    public decimal Budget { get; set; }
    public string? PreferredVenue { get; set; }
    public DateTime PreferredDate { get; set; }
    public TimeSpan EventDuration { get; set; }
    public string? Requirements { get; set; }
    public EventStatus Status { get; set; } = EventStatus.DRAFT;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
