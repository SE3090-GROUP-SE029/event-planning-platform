using Domain.Entities;
using Domain.Enums;

namespace Application.GuestManagement;

public class SeatAllocationService(
    IGuestRegistrationRepository repository,
    IGuestAiReviewRepository aiReviews,
    IRegistrationEligibilityPolicy eligibility,
    TimeProvider clock)
{
    public async Task<bool> ProcessNextAsync(CancellationToken ct)
    {
        var eventId = await repository.NextEventNeedingAllocationAsync(clock.GetUtcNow(), ct);
        if (eventId is null) return false;
        return await AllocateAsync(eventId.Value, ct);
    }

    public async Task<bool> AllocateAsync(Guid eventId, CancellationToken ct)
    {
        var changed = await repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            var form = await repository.FindFormAsync(eventId, ct);
            if (form is null || form.Status != RegistrationFormStatus.PUBLISHED) return false;

            var accepted = await repository.AcceptedAsync(eventId, ct);
            var now = clock.GetUtcNow();
            foreach (var registration in accepted)
            {
                registration.Status = RegistrationStatus.WAITLISTED;
                registration.UpdatedAt = now;
            }

            if (now >= EventEnd(eventDetails)) return accepted.Count > 0;

            var available = form.SeatLimit - await repository.ConfirmedCountAsync(eventId, ct);
            if (available <= 0) return accepted.Count > 0;

            var waiting = (await repository.WaitingAsync(eventId, ct))
                .Concat(accepted)
                .DistinctBy(r => r.Id)
                .OrderBy(r => r.RegisteredAt)
                .ThenBy(r => r.Id);
            var allocated = false;
            foreach (var registration in waiting)
            {
                if (available <= 0) break;
                if (!await HasCurrentAiAcceptanceAsync(registration, ct)) continue;
                if (!await eligibility.IsEligibleAsync(registration.Guest, eventDetails, ct)) continue;
                var confirmedAt = clock.GetUtcNow();
                registration.Status = RegistrationStatus.CONFIRMED;
                registration.ConfirmedAt = registration.UpdatedAt = confirmedAt;
                available--;
                allocated = true;
            }
            return allocated || accepted.Count > 0;
        }, ct);

        return changed;
    }

    private async Task<bool> HasCurrentAiAcceptanceAsync(
        RegistrationSubmission registration,
        CancellationToken ct)
    {
        if (registration.ReviewSource != ReviewSource.AI) return true;

        var review = await aiReviews.FindAsync(registration.Id, ct);
        return review is
        {
            Status: AiAnalysisStatus.COMPLETED,
            Decision: AiDecision.ACCEPTED,
            PromptVersion: GuestAiReviewVersions.CurrentPromptVersion
        };
    }

    private static DateTimeOffset EventEnd(Event eventDetails)
        => new(eventDetails.PreferredDate + eventDetails.EventDuration, TimeSpan.Zero);
}
