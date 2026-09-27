using Domain.Entities;

namespace Application.GuestManagement;

public interface IGuestRegistrationRepository
{
    // Holds a database event-row lock until the callback is committed; rolls back on failure.
    Task<T> WithEventLockAsync<T>(Guid eventId, Func<Event, Task<T>> action, CancellationToken cancellationToken);
    Task<RegistrationForm?> FindFormAsync(Guid eventId, CancellationToken cancellationToken);
    Task<RegistrationForm?> FindPublicFormAsync(string publicId, CancellationToken cancellationToken);
    Task<RegistrationSubmission?> FindRegistrationAsync(long id, CancellationToken cancellationToken);
    Task<RegistrationSubmission?> FindPublicRegistrationAsync(string reference, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(Guid eventId, string normalizedEmail, CancellationToken cancellationToken);
    Task<int> ConfirmedCountAsync(Guid eventId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RegistrationSubmission>> WaitingAsync(Guid eventId, CancellationToken cancellationToken);
    Task<RegistrationPage> ListAsync(Guid eventId, int page, int pageSize, CancellationToken cancellationToken);
    Task<bool> TokenExistsAsync(string value, CancellationToken cancellationToken);
    Task<Invitation?> FindInvitationByTokenAsync(string token, CancellationToken cancellationToken);
    Task<(Guid EventId, long Id)?> PendingDeliveryAsync(DateTimeOffset retryBefore, CancellationToken ct);
    void AddForm(RegistrationForm form);
    void AddQuestion(RegistrationQuestion question);
    void AddRegistration(RegistrationSubmission registration);
    void AddInvitation(Invitation invitation);
    Task SaveAsync(CancellationToken cancellationToken);
}
