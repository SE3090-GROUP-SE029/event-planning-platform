using Domain.Entities;
using Domain.Enums;

namespace Application.GuestManagement;

public class InvitationService(
    IGuestRegistrationRepository repository,
    IInvitationEmailSender emailSender,
    IRegistrationTokenGenerator tokens,
    IRegistrationSecretProtector secretProtector,
    GuestRegistrationOptions guestOptions,
    TimeProvider clock)
{
    public async Task EnsureForConfirmedAsync(Guid eventId, CancellationToken ct)
    {
        var invitations = await repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            var ids = await repository.ConfirmedWithoutInvitationAsync(eventId, ct);
            if (clock.GetUtcNow() >= EventEnd(eventDetails)) return Array.Empty<long>();

            foreach (var id in ids)
            {
                var registration = await RequireRegistrationAsync(eventId, id, ct);
                if (registration.Status != RegistrationStatus.CONFIRMED || registration.Invitation is not null)
                    continue;
                var invitation = new Invitation
                {
                    RegistrationSubmissionId = registration.Id,
                    RegistrationSubmission = registration,
                    Token = await UniqueTokenAsync(ct),
                    CreatedAt = clock.GetUtcNow(),
                    TokenExpiresAt = EventEnd(eventDetails)
                };
                registration.Invitation = invitation;
                repository.AddInvitation(invitation);
            }
            return ids;
        }, ct);

        await DeliverAsync(eventId, invitations, ct);
    }

    public async Task<bool> ProcessNextGenerationAsync(CancellationToken ct)
    {
        var eventId = await repository.NextEventNeedingInvitationAsync(clock.GetUtcNow(), ct);
        if (eventId is null) return false;
        await EnsureForConfirmedAsync(eventId.Value, ct);
        return true;
    }

    public async Task<RegistrationSubmission> RetryAsync(
        Guid eventId, string plannerId, long registrationId, CancellationToken ct)
    {
        var registration = await repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            RegistrationAccess.RequireOwner(eventDetails, plannerId);
            var row = await RequireRegistrationAsync(eventId, registrationId, ct);
            if (!IsActive(row))
                throw new RegistrationException(409, "invitation_unavailable", "No active confirmed invitation exists.");
            return row;
        }, ct);
        await DeliverAsync(eventId, [registrationId], ct);
        return await RequireRegistrationAsync(eventId, registrationId, ct);
    }

    public Task<bool> ValidateAsync(Guid eventId, string plannerId, string token, CancellationToken ct)
        => repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            RegistrationAccess.RequireOwner(eventDetails, plannerId);
            RegistrationValidator.ValidatePublicCredential(token);
            var invitation = await repository.FindInvitationByTokenAsync(token, ct);
            return invitation is not null &&
                invitation.RegistrationSubmission.EventId == eventId &&
                IsActive(invitation.RegistrationSubmission);
        }, ct);

    public async Task<bool> ProcessPendingDeliveryAsync(CancellationToken ct)
    {
        var pending = await repository.PendingInvitationDeliveryAsync(clock.GetUtcNow().AddMinutes(-1), ct);
        if (pending is null) return false;
        await DeliverAsync(pending.Value.EventId, [pending.Value.Id], ct);
        return true;
    }

    public bool IsActive(RegistrationSubmission registration)
        => registration.Status == RegistrationStatus.CONFIRMED &&
            registration.Invitation is { RevokedAt: null } invitation &&
            clock.GetUtcNow() < invitation.TokenExpiresAt;

    private async Task DeliverAsync(Guid eventId, IEnumerable<long> registrationIds,
        CancellationToken ct)
    {
        foreach (var id in registrationIds)
        {
            if (ct.IsCancellationRequested) return;
            await repository.WithEventLockAsync(eventId, async eventDetails =>
            {
                var registration = await RequireRegistrationAsync(eventId, id, ct);
                if (!IsActive(registration) || registration.Invitation!.DeliveryStatus == InvitationDeliveryStatus.SENT)
                    return false;
                var invitation = registration.Invitation;
                invitation.LastAttemptAt = clock.GetUtcNow();
                invitation.DeliveryAttempts++;
                EmailDeliveryResult delivery;
                try
                {
                    delivery = await emailSender.SendAsync(new InvitationEmail(
                        registration.Guest.EmailAddress,
                        registration.Guest.FullName,
                        eventDetails.EventName,
                        new DateTimeOffset(eventDetails.PreferredDate, TimeSpan.Zero),
                        eventDetails.PreferredVenue,
                        invitation.Token,
                        tokens.CreateQrPng(invitation.Token),
                        CreateStatusUrl(registration)), ct);
                }
                catch (Exception) when (!ct.IsCancellationRequested)
                {
                    delivery = EmailDeliveryResult.FAILED;
                }

                invitation.DeliveryStatus = delivery switch
                {
                    EmailDeliveryResult.SENT => InvitationDeliveryStatus.SENT,
                    EmailDeliveryResult.UNAVAILABLE => InvitationDeliveryStatus.PENDING,
                    _ => InvitationDeliveryStatus.FAILED
                };
                if (delivery == EmailDeliveryResult.SENT) invitation.SentAt = clock.GetUtcNow();
                return true;
            }, ct);
        }
    }

    private async Task<RegistrationSubmission> RequireRegistrationAsync(Guid eventId, long id, CancellationToken ct)
        => await repository.FindRegistrationAsync(id, ct) is { } registration && registration.EventId == eventId
            ? registration
            : throw new RegistrationException(404, "not_found", "Registration resource not found.");

    private async Task<string> UniqueTokenAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var token = tokens.Generate();
            if (!await repository.TokenExistsAsync(token, ct)) return token;
        }
        throw new RegistrationException(503, "token_generation_failed", "Unable to create a unique registration reference. Please retry.");
    }

    private string? CreateStatusUrl(RegistrationSubmission registration)
    {
        if (registration.ProtectedStatusSecret is null) return null;
        var secret = secretProtector.Unprotect(registration.ProtectedStatusSecret);
        var baseUri = guestOptions.GetPublicWebBaseUri();
        var url = new Uri(baseUri, $"guest/status/{Uri.EscapeDataString(registration.PublicReference)}");
        return new UriBuilder(url) { Fragment = $"secret={Uri.EscapeDataString(secret)}" }.Uri.AbsoluteUri;
    }

    private static DateTimeOffset EventEnd(Event eventDetails)
        => new(eventDetails.PreferredDate + eventDetails.EventDuration, TimeSpan.Zero);
}
