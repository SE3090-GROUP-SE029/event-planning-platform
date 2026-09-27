using Domain.Enums;

namespace Domain.Entities;

public class Invitation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long RegistrationSubmissionId { get; set; }
    public RegistrationSubmission RegistrationSubmission { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset TokenExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public RsvpStatus RsvpStatus { get; set; } = RsvpStatus.NOT_RESPONDED;
    public DateTimeOffset? RsvpedAt { get; set; }
    public InvitationDeliveryStatus DeliveryStatus { get; set; }
    public int DeliveryAttempts { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
}
