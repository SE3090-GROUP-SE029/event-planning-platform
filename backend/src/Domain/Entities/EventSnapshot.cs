using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>Immutable event data captured when a plan is generated.</summary>
public sealed class EventSnapshot
{
    private EventSnapshot() { }

    public EventSnapshot(
        string eventName,
        EventType eventType,
        DateTime eventDate,
        string location,
        int guestCount,
        decimal budget,
        IEnumerable<string> requirements,
        DateTime createdAt,
        DateTime lastModifiedAt)
    {
        EventName = RequiredText(eventName, nameof(eventName));
        Location = RequiredText(location, nameof(location));
        if (!Enum.IsDefined(eventType))
            throw new ArgumentOutOfRangeException(nameof(eventType));
        if (guestCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(guestCount));
        if (budget <= 0)
            throw new ArgumentOutOfRangeException(nameof(budget));
        if (eventDate <= DateTime.UtcNow)
            throw new ArgumentException("Event date must be in the future.", nameof(eventDate));

        EventType = eventType;
        EventDate = eventDate;
        GuestCount = guestCount;
        Budget = budget;
        Requirements = requirements?.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()).ToList()
            ?? throw new ArgumentNullException(nameof(requirements));
        CreatedAt = createdAt;
        LastModifiedAt = lastModifiedAt;
    }

    [Required, MaxLength(500)]
    public string EventName { get; init; } = string.Empty;
    public EventType EventType { get; init; }
    public DateTime EventDate { get; init; }

    [Required, MaxLength(500)]
    public string Location { get; init; } = string.Empty;
    public int GuestCount { get; init; }
    public decimal Budget { get; init; }
    public List<string> Requirements { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime LastModifiedAt { get; init; }

    public static EventSnapshot FromEvent(Event evt)
    {
        ArgumentNullException.ThrowIfNull(evt);
        return new EventSnapshot(
            evt.PreferredVenue ?? "Not specified",
            evt.EventType,
            evt.PreferredDate,
            evt.PreferredVenue ?? "Not specified",
            evt.GuestCount,
            evt.Budget,
            evt.Requirements?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [],
            evt.CreatedAt,
            evt.UpdatedAt ?? evt.CreatedAt);
    }

    public override string ToString() => $"{EventName} ({EventDate:O}, {GuestCount} guests)";

    private static string RequiredText(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", parameterName)
            : value.Trim();
}
