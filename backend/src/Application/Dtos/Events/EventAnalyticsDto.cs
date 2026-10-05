namespace Application.Dtos.Events;

public sealed record EventAnalyticsDto(
    int TotalRegistrations,
    int PendingAi,
    int Accepted,
    int Rejected,
    int Waitlisted,
    int RsvpAccepted,
    int RsvpDeclined,
    int RsvpMaybe,
    int CheckedIn,
    int NotCheckedIn,
    int AvailableSeats,
    int Capacity
);
