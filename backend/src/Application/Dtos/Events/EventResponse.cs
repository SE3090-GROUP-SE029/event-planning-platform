using Domain.Enums;

namespace Application.Dtos.Events;

public class EventResponse
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string EventName { get; set; } = default!;
    public EventType EventType { get; set; }
    public int GuestCount { get; set; }
    public decimal Budget { get; set; }
    public string PreferredVenue { get; set; } = default!;
    public DateTime EventDate { get; set; }
    public DateTime PreferredDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public TimeSpan EventDuration { get; set; }
    public string? Requirements { get; set; }
    public EventStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
