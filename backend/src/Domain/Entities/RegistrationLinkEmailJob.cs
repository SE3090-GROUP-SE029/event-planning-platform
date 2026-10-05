using Domain.Enums;

namespace Domain.Entities;

public class RegistrationLinkEmailJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public string EmailAddress { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string RegistrationUrl { get; set; } = string.Empty;
    public RegistrationLinkEmailDeliveryStatus Status { get; set; } = RegistrationLinkEmailDeliveryStatus.QUEUED;
    public int AttemptCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public string? LastFailureCode { get; set; }
}
