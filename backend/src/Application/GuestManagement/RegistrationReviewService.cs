using Domain.Entities;
using Domain.Enums;

namespace Application.GuestManagement;

public class RegistrationReviewService(
    IGuestRegistrationRepository repository,
    IGuestAiReviewRepository aiReviews,
    IRegistrationOutcomeEmailSender emailSender,
    TimeProvider clock)
{
    public async Task<RegistrationSubmission> DecideManuallyAsync(
        Guid eventId, string plannerId, long registrationId, RegistrationDecision decision, CancellationToken ct)
    {
        if (!Enum.IsDefined(decision))
            throw new RegistrationException(400, "invalid_review_decision", "Decision must be ACCEPTED or REJECTED.");

        var result = await repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            RegistrationAccess.RequireOwner(eventDetails, plannerId);
            var registration = await RequireRegistrationAsync(eventId, registrationId, ct);
            if (registration.Status is not (RegistrationStatus.PENDING_REVIEW or RegistrationStatus.ACCEPTED or RegistrationStatus.REJECTED))
                throw new RegistrationException(409, "review_unavailable", "Only unallocated registrations can be reviewed.");
            if (decision == RegistrationDecision.ACCEPTED &&
                registration.RejectionDeliveryStatus == InvitationDeliveryStatus.SENT)
                throw new RegistrationException(409, "review_unavailable", "A rejection email was already sent for this registration.");

            registration.Status = ToStatus(decision);
            registration.ReviewDecision = decision;
            registration.ReviewSource = ReviewSource.MANUAL;
            registration.ReviewedAt = registration.UpdatedAt = clock.GetUtcNow();
            await aiReviews.SupersedeAsync(registrationId, ct);
            if (decision == RegistrationDecision.REJECTED)
                registration.RejectionDeliveryStatus = InvitationDeliveryStatus.PENDING;
            else
            {
                registration.RejectionDeliveryStatus = null;
                registration.RejectionDeliveryAttempts = 0;
                registration.RejectionLastAttemptAt = null;
                registration.RejectionSentAt = null;
            }
            return registration;
        }, ct);

        if (decision == RegistrationDecision.REJECTED)
            await DeliverRejectionAsync(eventId, registrationId, ct);
        return result;
    }

    public async Task ApplyAiDecisionAsync(GuestAiClaim claim, GuestAiDecision decision, CancellationToken ct)
    {
        decision.Validate();
        var lookup = await repository.FindRegistrationAsync(claim.RegistrationSubmissionId, ct)
            ?? throw new RegistrationException(404, "not_found", "Registration resource not found.");

        var rejected = await repository.WithEventLockAsync(lookup.EventId, async _ =>
        {
            var registration = await RequireRegistrationAsync(lookup.EventId, lookup.Id, ct);
            if (!await aiReviews.CompleteAsync(claim, decision, clock.GetUtcNow(), ct)) return false;
            if (registration.Status != RegistrationStatus.PENDING_REVIEW) return false;

            var outcome = decision.Decision == AiDecision.ACCEPTED
                ? RegistrationDecision.ACCEPTED
                : RegistrationDecision.REJECTED;
            registration.Status = ToStatus(outcome);
            registration.ReviewDecision = outcome;
            registration.ReviewSource = ReviewSource.AI;
            registration.ReviewedAt = registration.UpdatedAt = clock.GetUtcNow();
            if (outcome == RegistrationDecision.REJECTED)
                registration.RejectionDeliveryStatus = InvitationDeliveryStatus.PENDING;
            return outcome == RegistrationDecision.REJECTED;
        }, ct);

        if (rejected) await DeliverRejectionAsync(lookup.EventId, lookup.Id, ct);
    }

    public async Task<RegistrationSubmission> RetryRejectionAsync(
        Guid eventId, string plannerId, long registrationId, CancellationToken ct)
    {
        await repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            RegistrationAccess.RequireOwner(eventDetails, plannerId);
            var registration = await RequireRegistrationAsync(eventId, registrationId, ct);
            if (registration.Status != RegistrationStatus.REJECTED)
                throw new RegistrationException(409, "rejection_unavailable", "Only a rejected registration can receive a rejection email.");
            return true;
        }, ct);
        await DeliverRejectionAsync(eventId, registrationId, ct, retry: true);
        return await RequireRegistrationAsync(eventId, registrationId, ct);
    }

    public async Task<bool> ProcessPendingRejectionEmailAsync(CancellationToken ct)
    {
        var pending = await repository.PendingRejectionDeliveryAsync(clock.GetUtcNow().AddMinutes(-1), ct);
        if (pending is null) return false;
        await DeliverRejectionAsync(pending.Value.EventId, pending.Value.Id, ct);
        return true;
    }

    private async Task<bool> DeliverRejectionAsync(Guid eventId, long id, CancellationToken ct, bool retry = false)
        => await repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            var registration = await RequireRegistrationAsync(eventId, id, ct);
            if (registration.Status != RegistrationStatus.REJECTED ||
                registration.RejectionDeliveryStatus == InvitationDeliveryStatus.SENT ||
                (!retry && (registration.RejectionDeliveryStatus == InvitationDeliveryStatus.FAILED ||
                    registration.RejectionLastAttemptAt > clock.GetUtcNow().AddMinutes(-1))))
                return false;

            registration.RejectionLastAttemptAt = clock.GetUtcNow();
            registration.RejectionDeliveryAttempts++;
            EmailDeliveryResult delivery;
            try
            {
                delivery = await emailSender.SendRejectionAsync(
                    new RejectionEmail(registration.Guest.EmailAddress, registration.Guest.FullName, eventDetails.EventName), ct);
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                delivery = EmailDeliveryResult.FAILED;
            }
            registration.RejectionDeliveryStatus = delivery switch
            {
                EmailDeliveryResult.SENT => InvitationDeliveryStatus.SENT,
                EmailDeliveryResult.UNAVAILABLE => InvitationDeliveryStatus.PENDING,
                _ => InvitationDeliveryStatus.FAILED
            };
            if (delivery == EmailDeliveryResult.SENT) registration.RejectionSentAt = clock.GetUtcNow();
            return true;
        }, ct);

    private async Task<RegistrationSubmission> RequireRegistrationAsync(Guid eventId, long id, CancellationToken ct)
        => await repository.FindRegistrationAsync(id, ct) is { } registration && registration.EventId == eventId
            ? registration
            : throw new RegistrationException(404, "not_found", "Registration resource not found.");

    private static RegistrationStatus ToStatus(RegistrationDecision decision)
        => decision == RegistrationDecision.ACCEPTED ? RegistrationStatus.ACCEPTED : RegistrationStatus.REJECTED;
}
