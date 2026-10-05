using Domain.Entities;
using Domain.Enums;

namespace Application.GuestManagement;

public interface IGuestRegistrationRepository
{
    // Holds a database event-row lock until the callback is committed; rolls back on failure.
    Task<T> WithEventLockAsync<T>(Guid eventId, Func<Event, Task<T>> action, CancellationToken cancellationToken);
    Task<RegistrationForm?> FindFormAsync(Guid eventId, CancellationToken cancellationToken);
    Task<RegistrationForm?> FindPublicFormAsync(string publicId, CancellationToken cancellationToken);
    Task<RegistrationSubmission?> FindRegistrationAsync(long id, CancellationToken cancellationToken);
    Task<RegistrationSubmission?> FindPublicRegistrationAsync(string reference, CancellationToken cancellationToken);
    Task<Guest?> FindGuestByEmailAsync(Guid eventId, string normalizedEmail, CancellationToken cancellationToken);
    Task<bool> RegistrationExistsAsync(Guid eventId, string normalizedEmail, CancellationToken cancellationToken);
    Task<IReadOnlySet<string>> RegisteredEmailsAsync(
        Guid eventId,
        IReadOnlyCollection<string> normalizedEmails,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Guest>> FindGuestsByEmailsAsync(
        Guid eventId,
        IReadOnlyCollection<string> normalizedEmails,
        CancellationToken cancellationToken);
    Task<int> ConfirmedCountAsync(Guid eventId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RegistrationSubmission>> AcceptedAsync(Guid eventId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RegistrationSubmission>> WaitingAsync(Guid eventId, CancellationToken cancellationToken);
    Task<Guid?> NextEventNeedingAllocationAsync(DateTimeOffset now, CancellationToken cancellationToken);
    Task<Guid?> NextEventNeedingInvitationAsync(DateTimeOffset now, CancellationToken cancellationToken);
    Task<IReadOnlyList<long>> ConfirmedWithoutInvitationAsync(Guid eventId, CancellationToken cancellationToken);
    Task<RegistrationPage> ListAsync(Guid eventId, int page, int pageSize, CancellationToken cancellationToken, RegistrationStatus? status = null, RsvpStatus? rsvpStatus = null, bool? isWaitlisted = null, bool? checkedIn = null);
    Task<bool> TokenExistsAsync(string value, CancellationToken cancellationToken);
    Task<Invitation?> FindInvitationByTokenAsync(string token, CancellationToken cancellationToken);
    Task<(Guid EventId, long Id)?> PendingInvitationDeliveryAsync(DateTimeOffset retryBefore, CancellationToken ct);
    Task<(Guid EventId, long Id)?> PendingRejectionDeliveryAsync(DateTimeOffset retryBefore, CancellationToken ct);
    Task<RegistrationLinkEmailJob?> ClaimNextRegistrationLinkEmailJobAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);
    Task CompleteRegistrationLinkEmailJobAsync(
        Guid jobId,
        EmailDeliveryResult result,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken);
    void AddForm(RegistrationForm form);
    void AddQuestion(RegistrationQuestion question);
    void AddGuest(Guest guest);
    void AddGuests(IEnumerable<Guest> guests);
    void AddRegistrationLinkEmailJobs(IEnumerable<RegistrationLinkEmailJob> jobs);
    void AddRegistration(RegistrationSubmission registration);
    void AddInvitation(Invitation invitation);
    Task<Application.Dtos.Events.EventAnalyticsDto> GetEventAnalyticsAsync(Guid eventId, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
}
