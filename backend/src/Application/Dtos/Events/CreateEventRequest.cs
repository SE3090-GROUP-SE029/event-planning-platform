using Domain.Enums;

namespace Application.Dtos.Events;

public class CreateEventRequest
{
    public string EventName { get; set; } = default!;
    public EventType? EventType { get; set; }
    public int GuestCount { get; set; }
    public decimal Budget { get; set; }
    public string PreferredVenue { get; set; } = default!;
    public DateTime? EventDate { get; set; }
    public DateTime PreferredDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public TimeSpan EventDuration { get; set; }
    public string? Requirements { get; set; }
}
