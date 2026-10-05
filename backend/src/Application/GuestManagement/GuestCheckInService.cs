using Domain.Entities;
using Domain.Enums;

namespace Application.GuestManagement;

public class GuestCheckInService(
    IGuestRegistrationRepository repository,
    TimeProvider clock)
{
    public Task<RegistrationSubmission> CheckInAsync(
        Guid eventId, string plannerId, string token, CancellationToken ct)
        => repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            RegistrationAccess.RequireOwner(eventDetails, plannerId);
            RegistrationValidator.ValidatePublicCredential(token);
            var invitation = await repository.FindInvitationByTokenAsync(token, ct);
            if (invitation is null)
                throw new RegistrationException(400, "invalid_qr", "Invalid QR code. Invitation not found.");

            var registration = invitation.RegistrationSubmission;
            if (registration.EventId != eventId)
                throw new RegistrationException(400, "wrong_event", "This invitation is for a different event.");
            if (registration.Status == RegistrationStatus.CANCELLED || invitation.RevokedAt is not null)
                throw new RegistrationException(400, "cancelled_invitation", "This invitation has been cancelled.");
            if (registration.Status == RegistrationStatus.REJECTED)
                throw new RegistrationException(400, "rejected_guest", "This guest's registration was rejected.");
            if (registration.Status is RegistrationStatus.PENDING_REVIEW or RegistrationStatus.ACCEPTED or RegistrationStatus.WAITLISTED)
                throw new RegistrationException(400, "waitlisted_guest", "This guest is pending review or does not have a confirmed seat.");
            if (clock.GetUtcNow() > invitation.TokenExpiresAt)
                throw new RegistrationException(400, "expired_qr", "This QR code has expired.");
            if (registration.Status != RegistrationStatus.CONFIRMED ||
                registration.Invitation is not { RevokedAt: null } activeInvitation ||
                clock.GetUtcNow() >= activeInvitation.TokenExpiresAt)
                throw new RegistrationException(400, "invalid_qr", "This invitation is no longer active.");
            if (registration.CheckIn is not null)
                throw new RegistrationException(400, "already_checked_in", "Guest has already checked in.");

            registration.CheckIn = new GuestCheckIn
            {
                RegistrationSubmissionId = registration.Id,
                RegistrationSubmission = registration,
                CheckedInAt = clock.GetUtcNow(),
                Method = CheckedInMethod.QR_CODE
            };
            return registration;
        }, ct);
}
