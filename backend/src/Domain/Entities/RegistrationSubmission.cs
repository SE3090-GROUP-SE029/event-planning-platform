using Domain.Enums;

namespace Domain.Entities;

public class RegistrationSubmission
{
    public long Id { get; set; }
    public Guid EventId { get; set; }
    public Guid RegistrationFormId { get; set; }
    public Guid GuestId { get; set; }
    public RegistrationForm RegistrationForm { get; set; } = null!;
    public Guest Guest { get; set; } = null!;
    public RegistrationStatus Status { get; set; } = RegistrationStatus.PENDING_REVIEW;
    public string PublicReference { get; set; } = string.Empty;
    public string StatusSecretHash { get; set; } = string.Empty;
    public string? ProtectedStatusSecret { get; set; }
    public RegistrationDecision? ReviewDecision { get; set; }
    public ReviewSource? ReviewSource { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset RegisteredAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public Invitation? Invitation { get; set; }
    public List<RegistrationAnswer> Answers { get; set; } = [];
    public InvitationDeliveryStatus? RejectionDeliveryStatus { get; set; }
    public int RejectionDeliveryAttempts { get; set; }
    public DateTimeOffset? RejectionLastAttemptAt { get; set; }
    public DateTimeOffset? RejectionSentAt { get; set; }
    public GuestCheckIn? CheckIn { get; set; }
}
