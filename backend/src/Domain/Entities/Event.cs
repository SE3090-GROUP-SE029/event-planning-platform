namespace Domain.Entities;

// Minimal shared event contract; event creation remains the event module's responsibility.
public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CreatedByUserId { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string? RequirementNotes { get; set; }
    public DateTimeOffset EventStartDate { get; set; }
    public DateTimeOffset EventEndDate { get; set; }
    public string? PreferredLocation { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
