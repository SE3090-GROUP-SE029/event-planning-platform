using Domain.Entities;
using Domain.Enums;

namespace Application.GuestManagement;

public class RegistrationRsvpService(
    IGuestRegistrationRepository repository,
    IRegistrationTokenGenerator tokens,
    InvitationService invitations,
    SeatAllocationService allocation,
    TimeProvider clock)
{
    public async Task<RegistrationSubmission> RespondAsync(
        string reference, string secret, RsvpStatus response, CancellationToken ct)
    {
        if (response is not (RsvpStatus.ACCEPTED or RsvpStatus.DECLINED or RsvpStatus.MAYBE))
            throw new RegistrationException(400, "invalid_rsvp", "RSVP must be ACCEPTED, DECLINED, or MAYBE.");
        var lookup = await RegistrationAccess.GetPublicRegistrationAsync(repository, tokens, reference, secret, ct);
        var declined = response == RsvpStatus.DECLINED;
        var registration = await repository.WithEventLockAsync(lookup.EventId, async _ =>
        {
            var row = await RegistrationAccess.GetPublicRegistrationAsync(repository, tokens, reference, secret, ct);
            if (!invitations.IsActive(row))
                throw new RegistrationException(409, "invitation_unavailable", "Only an active confirmed invitation can respond to an RSVP.");

            if (declined)
            {
                RegistrationTransitions.Cancel(row, clock);
                await repository.SaveAsync(ct);
            }
            else
            {
                row.Invitation!.RsvpStatus = response;
                row.Invitation.RsvpedAt = row.UpdatedAt = clock.GetUtcNow();
            }
            return row;
        }, ct);

        if (declined) await allocation.AllocateAsync(lookup.EventId, ct);
        return registration;
    }
}
