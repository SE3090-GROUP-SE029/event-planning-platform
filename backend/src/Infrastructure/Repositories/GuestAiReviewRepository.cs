using Application.GuestManagement;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories;

public class GuestAiReviewRepository(AppDbContext db, IGuestRegistrationRepository registrations,
    ILogger<GuestAiReviewRepository> logger) : IGuestAiReviewRepository
{
    public Task<GuestAiReview?> FindAsync(long registrationId, CancellationToken ct)
        => db.GuestAiReviews.AsNoTracking().SingleOrDefaultAsync(r => r.RegistrationSubmissionId == registrationId, ct);

    public Task<GuestAiReview> QueueAsync(Guid eventId, long registrationId, DateTimeOffset now, CancellationToken ct)
        => registrations.WithEventLockAsync(eventId, async _ =>
        {
            var registration = await db.RegistrationSubmissions.SingleAsync(r => r.Id == registrationId && r.EventId == eventId, ct);
            if (registration.Status is not (RegistrationStatus.PENDING_AI or RegistrationStatus.WAITING_LIST))
                return await FindAsync(registrationId, ct) ?? throw new RegistrationException(409, "analysis_unavailable", "Only pending registrations can be analyzed.");
            // Serialize retries with worker claims on the review row as well as creation on the event row.
            var rows = await db.GuestAiReviews.FromSqlInterpolated(
                $"SELECT * FROM \"GuestAiReviews\" WHERE \"RegistrationSubmissionId\" = {registrationId} FOR UPDATE").ToListAsync(ct);
            var review = rows.SingleOrDefault();
            if (review is null)
            {
                review = new GuestAiReview { RegistrationSubmission = registration, RequestedAt = now };
                db.GuestAiReviews.Add(review);
            }
            else if (review.Status == AiAnalysisStatus.COMPLETED && review.Decision == null || review.Status == AiAnalysisStatus.FAILED ||
                review.Status == AiAnalysisStatus.PROCESSING && review.LeaseExpiresAt <= now)
            {
                review.Status = AiAnalysisStatus.PENDING;
                review.RequestedAt = now;
                review.AttemptId = null;
                review.LeaseExpiresAt = null;
                review.FailureCode = null;
                review.Confidence = null;
                review.AnalyzedAt = null;
                review.Reasons = [];
                review.Flags = [];
            }
            logger.LogInformation("AI review retry requested for registration {RegistrationId}; status {Status}", registrationId, review.Status);
            return review;
        }, ct);

    public async Task<GuestAiClaim?> ClaimAsync(DateTimeOffset now, DateTimeOffset leaseExpiresAt, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Legacy waiting-list records need a fresh binding decision, including records predating reviews.
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "GuestAiReviews" ("Id", "RegistrationSubmissionId", "Status", "Reasons", "Flags", "RequestedAt", "Attempts")
            SELECT gen_random_uuid(), r."Id", 'PENDING', ARRAY[]::text[], ARRAY[]::text[], {now}, 0
            FROM "RegistrationSubmissions" r WHERE r."Status" = 'WAITING_LIST'
            AND NOT EXISTS (SELECT 1 FROM "GuestAiReviews" a WHERE a."RegistrationSubmissionId" = r."Id")
            ON CONFLICT ("RegistrationSubmissionId") DO NOTHING
            """, ct);
        var rows = await db.GuestAiReviews.FromSqlInterpolated($"""
            SELECT a.* FROM "GuestAiReviews" a
            JOIN "RegistrationSubmissions" r ON r."Id" = a."RegistrationSubmissionId"
            WHERE r."Status" IN ('PENDING_AI', 'WAITING_LIST') AND
            (a."Status" = 'PENDING' OR (a."Status" = 'PROCESSING' AND a."LeaseExpiresAt" <= {now})
             OR (a."Status" = 'COMPLETED' AND a."Decision" IS NULL))
            ORDER BY a."RequestedAt", a."Id" LIMIT 1 FOR UPDATE OF a SKIP LOCKED
            """).ToListAsync(ct);
        var review = rows.SingleOrDefault();
        if (review is null) return null;
        review.Status = AiAnalysisStatus.PROCESSING;
        review.Confidence = null;
        review.AnalyzedAt = null;
        review.Reasons = [];
        review.Flags = [];
        review.FailureCode = null;
        review.AttemptId = Guid.NewGuid();
        review.LeaseExpiresAt = leaseExpiresAt;
        review.Attempts++;
        var claim = new GuestAiClaim(review.Id, review.RegistrationSubmissionId, review.AttemptId.Value);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        db.ChangeTracker.Clear();
        logger.LogInformation("AI analysis requested for review {ReviewId}, attempt {AttemptId}", claim.ReviewId, claim.AttemptId);
        return claim;
    }

    public async Task<GuestAiContext> ContextAsync(long registrationId, CancellationToken ct)
    {
        var submission = await db.RegistrationSubmissions.AsNoTracking().Where(r => r.Id == registrationId)
            .Select(r => new { r.EventId, r.RegistrationFormId, r.RegisteredAt,
                Guest = new AiGuestContext(r.Guest.FullName, r.Guest.EmailAddress, r.Guest.Organisation, r.Guest.PhoneNumber),
                Event = new AiEventContext(r.RegistrationForm.Event.EventName, r.RegistrationForm.Event.Requirements) })
            .SingleAsync(ct);
        // Selection only, never eligibility rules. Include likely matches first, then recent prior submissions.
        var emailPrefix = submission.Guest.EmailAddress.Split('@')[0] + "@";
        var candidates = await db.RegistrationSubmissions.AsNoTracking()
            .Where(r => r.EventId == submission.EventId && r.Id < registrationId)
            .OrderByDescending(r => r.Guest.FullName == submission.Guest.FullName ||
                (submission.Guest.PhoneNumber != null && r.Guest.PhoneNumber == submission.Guest.PhoneNumber) ||
                r.Guest.EmailAddress.StartsWith(emailPrefix))
            .ThenByDescending(r => r.RegisteredAt).ThenByDescending(r => r.Id).Take(21)
            .Select(r => new AiComparisonContext(
                new AiGuestContext(r.Guest.FullName, r.Guest.EmailAddress, r.Guest.Organisation, r.Guest.PhoneNumber), r.RegisteredAt))
            .ToListAsync(ct);
        var answers = await db.RegistrationAnswers.AsNoTracking().Where(a => a.RegistrationSubmissionId == registrationId)
            .ToDictionaryAsync(a => a.RegistrationQuestionId, a => a.Answer, ct);
        var selected = await db.RegistrationQuestions.AsNoTracking().Where(q => q.RegistrationFormId == submission.RegistrationFormId && q.IsSelected)
            .OrderBy(q => q.DisplayOrder).ToListAsync(ct);
        return new GuestAiContext(submission.Guest, submission.Event, submission.RegisteredAt,
            candidates.Take(20).ToArray(), candidates.Count > 20,
            selected.Select(q => new AiQuestionAnswer(q.Question, q.Required, answers.GetValueOrDefault(q.Id))).ToArray());
    }

    private IQueryable<GuestAiReview> OwnedClaim(GuestAiClaim claim)
        => db.GuestAiReviews.Where(r => r.Id == claim.ReviewId && r.Status == AiAnalysisStatus.PROCESSING && r.AttemptId == claim.AttemptId);

    public async Task<bool> CompleteAsync(GuestAiClaim claim, GuestAiDecision decision, DateTimeOffset now, CancellationToken ct)
    {
        decision.Validate();
        var updated = await OwnedClaim(claim).ExecuteUpdateAsync(setters => setters
            .SetProperty(r => r.Status, AiAnalysisStatus.COMPLETED)
            .SetProperty(r => r.Decision, (AiDecision?)decision.Decision)
            .SetProperty(r => r.Confidence, (double?)decision.Confidence)
            .SetProperty(r => r.Reasons, decision.Reasons).SetProperty(r => r.Flags, decision.Flags)
            .SetProperty(r => r.Model, decision.Model).SetProperty(r => r.PromptVersion, decision.PromptVersion)
            .SetProperty(r => r.AnalyzedAt, now).SetProperty(r => r.LeaseExpiresAt, (DateTimeOffset?)null)
            .SetProperty(r => r.FailureCode, (string?)null), ct);
        if (updated == 1) logger.LogInformation("AI analysis completed for review {ReviewId}: {Decision}", claim.ReviewId, decision.Decision);
        return updated == 1;
    }

    public async Task<bool> FailAsync(GuestAiClaim claim, string code, CancellationToken ct)
    {
        var updated = await OwnedClaim(claim).ExecuteUpdateAsync(setters => setters
            .SetProperty(r => r.Status, AiAnalysisStatus.FAILED).SetProperty(r => r.FailureCode, code)
            .SetProperty(r => r.LeaseExpiresAt, (DateTimeOffset?)null), ct);
        if (updated == 1) logger.LogWarning("AI analysis failed for review {ReviewId}: {FailureCode}", claim.ReviewId, code);
        return updated == 1;
    }
}
