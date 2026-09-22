using Domain.Entities;

namespace Application.GuestManagement;

public class GuestAiReviewService(IGuestAiReviewRepository repository, IGuestAiClient client,
    RegistrationService registrations, TimeProvider clock)
{
    public async Task<GuestAiReview> GetAsync(Guid eventId, string plannerId, long registrationId, CancellationToken ct)
    {
        await registrations.GetRegistrationAsync(eventId, plannerId, registrationId, ct);
        return await repository.FindAsync(registrationId, ct)
            ?? throw new RegistrationException(404, "ai_review_not_found", "AI analysis has not been requested. The planner can request a retry.");
    }

    public async Task<GuestAiReview> RetryAsync(Guid eventId, string plannerId, long registrationId, CancellationToken ct)
    {
        await registrations.GetRegistrationAsync(eventId, plannerId, registrationId, ct);
        var review = await repository.QueueAsync(eventId, registrationId, clock.GetUtcNow(), ct);
        return review;
    }

    public async Task<bool> ProcessNextAsync(TimeSpan leaseDuration, CancellationToken ct)
    {
        if (await registrations.ProcessPendingDeliveryAsync(ct)) return true;
        var now = clock.GetUtcNow();
        var claim = await repository.ClaimAsync(now, now.Add(leaseDuration), ct);
        if (claim is null) return false;
        GuestAiDecision decision;
        try
        {
            var context = await repository.ContextAsync(claim.RegistrationSubmissionId, ct);
            decision = await client.AnalyzeAsync(context, ct);
            decision.Validate();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // A new worker can recover the durable claim after its lease expires.
            throw;
        }
        catch (Exception exception)
        {
            var code = exception is AiAnalysisException ai && ai.Code is
                "ai_unavailable" or "ai_timeout" or "invalid_ai_response" ? ai.Code : "analysis_failed";
            await repository.FailAsync(claim, code, ct);
            return true;
        }

        await registrations.ApplyDecisionAsync(claim, decision, repository, ct);
        return true;
    }
}
