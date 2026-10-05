using Domain.Entities;
using Domain.Enums;

namespace Application.GuestManagement;

internal static class RegistrationTransitions
{
    public static void Cancel(RegistrationSubmission registration, TimeProvider clock)
    {
        if (registration.Status == RegistrationStatus.REJECTED)
            throw new RegistrationException(409, "registration_rejected", "A rejected registration cannot be cancelled or respond to an invitation.");
        if (registration.Status == RegistrationStatus.CANCELLED) return;

        var now = clock.GetUtcNow();
        registration.Status = RegistrationStatus.CANCELLED;
        registration.CancelledAt = registration.UpdatedAt = now;
        if (registration.Invitation is { } invitation)
        {
            invitation.RevokedAt = now;
            invitation.RsvpStatus = RsvpStatus.DECLINED;
            invitation.RsvpedAt = now;
        }
    }
}
