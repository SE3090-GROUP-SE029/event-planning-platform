using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.GuestManagement;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Backend.IntegrationTests;

public class GuestAiReviewEndpointTests(RegistrationHostFixture fixture) : IClassFixture<RegistrationHostFixture>
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(3);
    private static GuestAiDecision Decision(AiDecision recommendation = AiDecision.ACCEPTED)
        => new(recommendation, 0.85, ["Evidence-based test recommendation."], recommendation == AiDecision.ACCEPTED ? [] : ["MANUAL_VERIFICATION"], "qwen3:8b", "guest-filtering-v2");

    private async Task DrainAsync()
    {
        for (var i = 0; i < 100; i++)
            if (!await fixture.WithAiServiceAsync(s => s.ProcessNextAsync(Lease, Ct))) return;
        throw new InvalidOperationException("Unexpected review backlog in test");
    }

    private async Task<(Event Event, string PublicId)> PrepareAsync(int seats = 1)
    {
        fixture.Ai.Failure = null;
        fixture.Ai.BeforeReturn = null;
        fixture.Ai.Decision = Decision();
        await DrainAsync();
        var eventDetails = await fixture.CreateEventAsync();
        await fixture.WithServiceAsync(s => s.CreateFormAsync(eventDetails.Id, "planner",
            new FormSettings(RegistrationHostFixture.Now.AddHours(-1), RegistrationHostFixture.Now.AddDays(5), seats), Ct));
        var form = await fixture.WithServiceAsync(s => s.PublishAsync(eventDetails.Id, "planner", Ct));
        return (eventDetails, form.PublicId!);
    }

    private Task<RegistrationReceipt> SubmitAsync(string publicId, int number = 1, string? organisation = null)
        => fixture.WithServiceAsync(s => s.SubmitAsync(publicId, new GuestDetails($"Guest {number}", $"guest{number}@example.com", organisation, null), Ct));

    private static string Path(Guid eventId, long id, bool retry = false)
        => $"/api/events/{eventId}/registration-form/registrations/{id}/" + (retry ? "retry-ai-review" : "ai-review");

    private async Task<HttpResponseMessage> SendAsync(string path, bool retry = false, string? owner = "planner", string role = "Planner")
    {
        using var request = new HttpRequestMessage(retry ? HttpMethod.Post : HttpMethod.Get, path);
        if (owner is not null) request.Headers.Add("X-Test-Identity", owner);
        request.Headers.Add("X-Test-Role", role);
        return await fixture.Client.SendAsync(request);
    }

    [Theory]
    [InlineData(AiDecision.ACCEPTED)]
    [InlineData(AiDecision.REJECTED)]
    public async Task PersistsDecisionsAndAutomaticallyAppliesCapacityOrRejection(AiDecision recommendation)
    {
        var (eventDetails, publicId) = await PrepareAsync();
        var first = await SubmitAsync(publicId);
        await DrainAsync();
        var second = await SubmitAsync(publicId, 2);
        fixture.Ai.Decision = Decision(recommendation);
        await DrainAsync();
        using var response = await SendAsync(Path(eventDetails.Id, second.Registration.Id));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("COMPLETED", result.GetProperty("status").GetString());
        Assert.Equal(recommendation.ToString(), result.GetProperty("decision").GetString());
        Assert.Equal(0.85, result.GetProperty("confidence").GetDouble());
        Assert.NotEqual(JsonValueKind.Null, result.GetProperty("analyzedAt").ValueKind);
        Assert.True(response.Headers.CacheControl!.NoStore);
        await using var db = fixture.CreateDb();
        Assert.Equal(RegistrationStatus.CONFIRMED, (await db.RegistrationSubmissions.FindAsync(first.Registration.Id))!.Status);
        Assert.Equal(recommendation == AiDecision.ACCEPTED ? RegistrationStatus.WAITING_LIST : RegistrationStatus.REJECTED,
            (await db.RegistrationSubmissions.FindAsync(second.Registration.Id))!.Status);
        if (recommendation == AiDecision.REJECTED)
            Assert.Contains(fixture.Email.Rejections, e => e.EmailAddress == second.Registration.Guest.EmailAddress);
        Assert.Single(await db.Invitations.Where(i => i.RegistrationSubmission.EventId == eventDetails.Id).ToListAsync());
        Assert.Equal(recommendation, (await db.GuestAiReviews.SingleAsync(r => r.RegistrationSubmissionId == second.Registration.Id)).Decision);
    }

    [Theory]
    [InlineData("ai_unavailable")]
    [InlineData("ai_timeout")]
    [InlineData("invalid_ai_response")]
    public async Task PublicRegistrationSurvivesAiFailureAndProtectedRetryCompletes(string code)
    {
        var (eventDetails, publicId) = await PrepareAsync();
        fixture.Ai.Failure = new AiAnalysisException(code);
        using var submitted = await fixture.Client.PostAsJsonAsync($"/api/public/registration-forms/{publicId}/registrations",
            new { fullName = "Guest", emailAddress = "guest@example.com" });
        Assert.Equal(HttpStatusCode.Created, submitted.StatusCode);
        await DrainAsync();
        await using var db = fixture.CreateDb();
        var registration = await db.RegistrationSubmissions.SingleAsync(r => r.EventId == eventDetails.Id);
        var failed = await db.GuestAiReviews.AsNoTracking().SingleAsync(r => r.RegistrationSubmissionId == registration.Id);
        Assert.Equal(AiAnalysisStatus.FAILED, failed.Status);
        Assert.Equal(code, failed.FailureCode);
        Assert.Null(failed.Decision);
        Assert.Null(failed.Confidence);
        Assert.Equal(RegistrationStatus.PENDING_AI, registration.Status);
        Assert.False(await db.Invitations.AnyAsync(i => i.RegistrationSubmissionId == registration.Id));
        fixture.Ai.Failure = null;
        using var retried = await SendAsync(Path(eventDetails.Id, registration.Id, true), true);
        Assert.Equal(HttpStatusCode.Accepted, retried.StatusCode);
        await DrainAsync();
        var completed = await db.GuestAiReviews.AsNoTracking().SingleAsync(r => r.Id == failed.Id);
        Assert.Equal(AiAnalysisStatus.COMPLETED, completed.Status);
        Assert.Equal(2, completed.Attempts);
        Assert.Null(completed.FailureCode);
        Assert.Equal(RegistrationStatus.CONFIRMED, (await db.RegistrationSubmissions.AsNoTracking().SingleAsync(r => r.Id == registration.Id)).Status);
    }

    [Fact]
    public async Task OptionalMissingInformationCanBeAcceptedWithoutInventingARejection()
    {
        var (_, publicId) = await PrepareAsync();
        var receipt = await SubmitAsync(publicId);
        fixture.Ai.Decision = new(AiDecision.ACCEPTED, 0.7, ["Missing optional details do not contradict any event requirement."], [], "qwen3:8b", "guest-filtering-v2");
        await DrainAsync();
        await using var db = fixture.CreateDb();
        Assert.Equal(AiDecision.ACCEPTED, (await db.GuestAiReviews.SingleAsync(r => r.RegistrationSubmissionId == receipt.Registration.Id)).Decision);
        Assert.Null(fixture.Ai.Contexts.Last().Guest.Organisation);
        Assert.Null(fixture.Ai.Contexts.Last().Guest.PhoneNumber);
    }

    [Fact]
    public async Task AnonymousWrongRoleWrongOwnerAndCrossEventRequestsAreDenied()
    {
        var (eventDetails, publicId) = await PrepareAsync();
        var receipt = await SubmitAsync(publicId);
        var otherEvent = await fixture.CreateEventAsync();
        foreach (var retry in new[] { false, true })
        {
            var path = Path(eventDetails.Id, receipt.Registration.Id, retry);
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(path, retry, owner: null)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(path, retry, role: "Guest")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(path, retry, owner: "other")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(Path(otherEvent.Id, receipt.Registration.Id, retry), retry)).StatusCode);
        }
    }

    [Fact]
    public async Task PublicReceiptAndStatusDoNotExposeAnyAiFields()
    {
        var (_, publicId) = await PrepareAsync();
        using var submitted = await fixture.Client.PostAsJsonAsync($"/api/public/registration-forms/{publicId}/registrations",
            new { fullName = "Guest", emailAddress = "guest@example.com" });
        var receipt = await submitted.Content.ReadFromJsonAsync<JsonElement>();
        fixture.Ai.Decision = Decision(AiDecision.REJECTED);
        await DrainAsync();
        using var status = await fixture.Client.PostAsJsonAsync($"/api/public/registrations/{receipt.GetProperty("publicReference").GetString()}/status",
            new { secret = receipt.GetProperty("statusSecret").GetString() });
        var current = await status.Content.ReadFromJsonAsync<JsonElement>();
        foreach (var field in new[] { "decision", "confidence", "reasons", "flags", "model", "aiReview", "failureCode" })
        {
            Assert.False(receipt.TryGetProperty(field, out _));
            Assert.False(current.TryGetProperty(field, out _));
        }
        Assert.Equal("REJECTED", current.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, current.GetProperty("invitationToken").ValueKind);
    }

    [Fact]
    public async Task WorkIsAtomicWithRegistrationAndValidationFailuresDoNotQueueAnything()
    {
        var (eventDetails, publicId) = await PrepareAsync();
        await Assert.ThrowsAsync<RegistrationException>(() => fixture.WithServiceAsync(s => s.SubmitAsync(publicId, new GuestDetails("", "invalid", null, null), Ct)));
        var receipt = await SubmitAsync(publicId);
        await Assert.ThrowsAsync<RegistrationException>(() => SubmitAsync(publicId));
        await using var db = fixture.CreateDb();
        var rows = await db.GuestAiReviews.Where(r => r.RegistrationSubmission.EventId == eventDetails.Id).ToListAsync();
        var review = Assert.Single(rows);
        Assert.Equal(receipt.Registration.Id, review.RegistrationSubmissionId);
        Assert.Equal(AiAnalysisStatus.PENDING, review.Status);
        Assert.Null(review.Decision);
    }

    [Fact]
    public async Task ConcurrentRetriesQueueOnceAndOnlyOneWorkerClaimsTheReview()
    {
        var (eventDetails, publicId) = await PrepareAsync();
        var receipt = await SubmitAsync(publicId);
        fixture.Ai.Failure = new AiAnalysisException("ai_unavailable");
        await DrainAsync();
        var replies = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => SendAsync(Path(eventDetails.Id, receipt.Registration.Id, true), true)));
        Assert.All(replies, response => Assert.Equal(HttpStatusCode.Accepted, response.StatusCode));
        var now = fixture.Clock.GetUtcNow();
        var claims = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => fixture.WithAiRepositoryAsync(r => r.ClaimAsync(now, now.Add(Lease), Ct))));
        var claim = Assert.Single(claims.Where(c => c is not null))!;
        Assert.True(await fixture.WithAiRepositoryAsync(r => r.CompleteAsync(claim, Decision(), now, Ct)));
        await using var db = fixture.CreateDb();
        var review = await db.GuestAiReviews.SingleAsync(r => r.RegistrationSubmissionId == receipt.Registration.Id);
        Assert.Equal(2, review.Attempts);
        using var repeated = await SendAsync(Path(eventDetails.Id, receipt.Registration.Id, true), true);
        Assert.Equal("COMPLETED", (await repeated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
    }

    [Fact]
    public async Task ExpiredClaimsAreRecoveredAndStaleResponsesCannotOverwriteTheNewAttempt()
    {
        var (_, publicId) = await PrepareAsync();
        await SubmitAsync(publicId);
        var now = fixture.Clock.GetUtcNow();
        var old = (await fixture.WithAiRepositoryAsync(r => r.ClaimAsync(now, now.AddSeconds(1), Ct)))!;
        var current = (await fixture.WithAiRepositoryAsync(r => r.ClaimAsync(now.AddSeconds(2), now.Add(Lease), Ct)))!;
        Assert.Equal(old.ReviewId, current.ReviewId);
        Assert.NotEqual(old.AttemptId, current.AttemptId);
        Assert.False(await fixture.WithAiRepositoryAsync(r => r.CompleteAsync(old, Decision(AiDecision.REJECTED), now, Ct)));
        Assert.False(await fixture.WithAiRepositoryAsync(r => r.FailAsync(old, "ai_timeout", Ct)));
        Assert.True(await fixture.WithAiRepositoryAsync(r => r.CompleteAsync(current, Decision(), now, Ct)));
    }

    [Fact]
    public async Task SlowModelDoesNotHoldAnEventLockOrDelayAnotherRegistration()
    {
        var (_, publicId) = await PrepareAsync();
        await SubmitAsync(publicId);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Ai.BeforeReturn = async ct => { started.TrySetResult(); await release.Task.WaitAsync(ct); };
        var analysis = fixture.WithAiServiceAsync(s => s.ProcessNextAsync(Lease, Ct));
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var second = await SubmitAsync(publicId, 2).WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(RegistrationStatus.PENDING_AI, second.Registration.Status);
        }
        finally { release.TrySetResult(); await analysis; fixture.Ai.BeforeReturn = null; }
    }

    [Fact]
    public async Task ContextIsBoundedPriorAndScopedToTheEvent()
    {
        var (_, publicId) = await PrepareAsync();
        RegistrationReceipt receipt = null!;
        for (var i = 0; i < 23; i++) receipt = await SubmitAsync(publicId, i);
        var context = await fixture.WithAiRepositoryAsync(r => r.ContextAsync(receipt.Registration.Id, Ct));
        Assert.Equal(20, context.Comparisons.Count);
        Assert.True(context.ComparisonsLimited);
        Assert.DoesNotContain(context.Comparisons, c => c.Guest.EmailAddress == receipt.Registration.Guest.EmailAddress);
        var (other, otherPublicId) = await PrepareAsync();
        var otherReceipt = await SubmitAsync(otherPublicId, 500);
        var otherContext = await fixture.WithAiRepositoryAsync(r => r.ContextAsync(otherReceipt.Registration.Id, Ct));
        Assert.Empty(otherContext.Comparisons);
        Assert.False(otherContext.ComparisonsLimited);
        Assert.Equal(other.RequirementNotes, otherContext.Event.RequirementNotes);
    }

    [Fact]
    public async Task NewMigrationMatchesTheModelAndEnforcesReviewConstraints()
    {
        var (_, publicId) = await PrepareAsync();
        var receipt = await SubmitAsync(publicId);
        await using var db = fixture.CreateDb();
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Contains(await db.Database.GetAppliedMigrationsAsync(), name => name.EndsWith("_AddGuestAiReviews"));
        var review = await db.GuestAiReviews.SingleAsync(r => r.RegistrationSubmissionId == receipt.Registration.Id);
        review.Confidence = 2;
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task ExistingRegistrationWithoutAReviewCanBeQueuedByItsPlanner()
    {
        var (eventDetails, publicId) = await PrepareAsync();
        // Reproduce a pre-feature record without removing anything from the database.
        await using var db = fixture.CreateDb();
        var form = await db.RegistrationForms.SingleAsync(f => f.PublicId == publicId);
        var tokens = new Infrastructure.ExternalServices.RegistrationTokenGenerator();
        var legacy = new RegistrationSubmission
        {
            EventId = eventDetails.Id, RegistrationForm = form,
            Guest = new Guest { EventId = eventDetails.Id, FullName = "Legacy Guest", EmailAddress = "legacy@example.com", NormalizedEmail = "LEGACY@EXAMPLE.COM" },
            PublicReference = tokens.Generate(), StatusSecretHash = tokens.Hash(tokens.Generate()),
            RegisteredAt = fixture.Clock.GetUtcNow(), UpdatedAt = fixture.Clock.GetUtcNow()
        };
        db.RegistrationSubmissions.Add(legacy);
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(Path(eventDetails.Id, legacy.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, (await SendAsync(Path(eventDetails.Id, legacy.Id, true), true)).StatusCode);
        await DrainAsync();
        Assert.Equal(AiAnalysisStatus.COMPLETED, (await db.GuestAiReviews.SingleAsync(r => r.RegistrationSubmissionId == legacy.Id)).Status);
    }

    [Fact]
    public async Task WorkerCancellationLeavesDurableWorkForLeaseRecovery()
    {
        var (_, publicId) = await PrepareAsync();
        var receipt = await SubmitAsync(publicId);
        using var cancellation = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Ai.BeforeReturn = async ct => { started.TrySetResult(); await Task.Delay(Timeout.Infinite, ct); };
        var processing = fixture.WithAiServiceAsync(s => s.ProcessNextAsync(Lease, cancellation.Token));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => processing);
        fixture.Ai.BeforeReturn = null;
        await using var db = fixture.CreateDb();
        var review = await db.GuestAiReviews.AsNoTracking().SingleAsync(r => r.RegistrationSubmissionId == receipt.Registration.Id);
        Assert.Equal(AiAnalysisStatus.PROCESSING, review.Status);
        Assert.Null(review.Decision);
        var afterLease = review.LeaseExpiresAt!.Value.AddSeconds(1);
        var claim = (await fixture.WithAiRepositoryAsync(r => r.ClaimAsync(afterLease, afterLease.Add(Lease), Ct)))!;
        Assert.True(await fixture.WithAiRepositoryAsync(r => r.CompleteAsync(claim, Decision(), afterLease, Ct)));
    }

    [Fact]
    public async Task RejectionEmailFailureRetriesWithoutRerunningAiOrCreatingQr()
    {
        var (eventDetails, publicId) = await PrepareAsync();
        var receipt = await SubmitAsync(publicId);
        fixture.Ai.Decision = Decision(AiDecision.REJECTED);
        fixture.Email.ThrowOnSend = true;
        await DrainAsync();
        var current = await fixture.WithServiceAsync(s => s.GetRegistrationAsync(eventDetails.Id, "planner", receipt.Registration.Id, Ct));
        Assert.Equal(RegistrationStatus.REJECTED, current.Status);
        Assert.Equal(InvitationDeliveryStatus.FAILED, current.RejectionDeliveryStatus);
        Assert.Null(current.Invitation);
        fixture.Email.ThrowOnSend = false;
        var retried = await fixture.WithServiceAsync(s => s.RetryRejectionAsync(eventDetails.Id, "planner", current.Id, Ct));
        Assert.Equal(InvitationDeliveryStatus.SENT, retried.RejectionDeliveryStatus);
        Assert.Equal(2, retried.RejectionDeliveryAttempts);
        var repeated = await fixture.WithServiceAsync(s => s.RetryRejectionAsync(eventDetails.Id, "planner", current.Id, Ct));
        Assert.Equal(2, repeated.RejectionDeliveryAttempts);
        await using var db = fixture.CreateDb();
        Assert.Equal(1, (await db.GuestAiReviews.SingleAsync(r => r.RegistrationSubmissionId == current.Id)).Attempts);
        Assert.False(await db.Invitations.AnyAsync(i => i.RegistrationSubmissionId == current.Id));
        var error = await Assert.ThrowsAsync<RegistrationException>(() => fixture.WithServiceAsync(s => s.RespondAsync(
            receipt.Registration.PublicReference, receipt.StatusSecret, RsvpStatus.ACCEPTED, Ct)));
        Assert.Equal(409, error.StatusCode);
    }

    [Theory]
    [InlineData(AiDecision.ACCEPTED)]
    [InlineData(AiDecision.REJECTED)]
    public async Task PendingEmailIsRecoveredWithoutReanalysis(AiDecision decision)
    {
        var (eventDetails, publicId) = await PrepareAsync();
        var receipt = await SubmitAsync(publicId);
        fixture.Ai.Decision = Decision(decision);
        fixture.Email.Result = EmailDeliveryResult.UNAVAILABLE;
        await DrainAsync();
        fixture.Email.Result = EmailDeliveryResult.SENT;
        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddMinutes(2);
        await DrainAsync();
        var current = await fixture.WithServiceAsync(s => s.GetRegistrationAsync(eventDetails.Id, "planner", receipt.Registration.Id, Ct));
        Assert.Equal(InvitationDeliveryStatus.SENT, decision == AiDecision.ACCEPTED ? current.Invitation!.DeliveryStatus : current.RejectionDeliveryStatus);
        await using var db = fixture.CreateDb();
        Assert.Equal(1, (await db.GuestAiReviews.SingleAsync(r => r.RegistrationSubmissionId == current.Id)).Attempts);
    }

    [Theory]
    [InlineData(AiDecision.ACCEPTED)]
    [InlineData(AiDecision.REJECTED)]
    public async Task ResultArrivingAfterCancellationCannotResurrectOrEmailGuest(AiDecision decision)
    {
        var (eventDetails, publicId) = await PrepareAsync();
        var receipt = await SubmitAsync(publicId);
        var now = fixture.Clock.GetUtcNow();
        var claim = (await fixture.WithAiRepositoryAsync(r => r.ClaimAsync(now, now.Add(Lease), Ct)))!;
        await fixture.WithServiceAsync(s => s.CancelAsync(eventDetails.Id, "planner", receipt.Registration.Id, Ct));
        var rejectedEmails = fixture.Email.Rejections.Count;
        var invitations = fixture.Email.Messages.Count;
        // Resolve both services in the same scope, matching worker transaction ownership.
        await fixture.ApplyDecisionAsync(claim, Decision(decision));
        var current = await fixture.WithServiceAsync(s => s.GetRegistrationAsync(eventDetails.Id, "planner", receipt.Registration.Id, Ct));
        Assert.Equal(RegistrationStatus.CANCELLED, current.Status);
        Assert.Null(current.Invitation);
        Assert.Equal(invitations, fixture.Email.Messages.Count);
        Assert.Equal(rejectedEmails, fixture.Email.Rejections.Count);
    }

    [Fact]
    public async Task RejectedAndPendingGuestsAreNeverPromotedAheadOfAcceptedWaiters()
    {
        var (eventDetails, publicId) = await PrepareAsync();
        var confirmed = await SubmitAsync(publicId, 1);
        await DrainAsync();
        var rejected = await SubmitAsync(publicId, 2);
        fixture.Ai.Decision = Decision(AiDecision.REJECTED);
        await DrainAsync();
        var waiter = await SubmitAsync(publicId, 3);
        fixture.Ai.Decision = Decision();
        await DrainAsync();
        var pending = await SubmitAsync(publicId, 4);
        await fixture.WithServiceAsync(s => s.CancelAsync(eventDetails.Id, "planner", confirmed.Registration.Id, Ct));
        await using var db = fixture.CreateDb();
        Assert.Equal(RegistrationStatus.REJECTED, (await db.RegistrationSubmissions.FindAsync(rejected.Registration.Id))!.Status);
        Assert.Equal(RegistrationStatus.PENDING_AI, (await db.RegistrationSubmissions.FindAsync(pending.Registration.Id))!.Status);
        Assert.Equal(RegistrationStatus.CONFIRMED, (await db.RegistrationSubmissions.FindAsync(waiter.Registration.Id))!.Status);
    }

    [Fact]
    public async Task LegacyAdviceDoesNotAuthorizePromotionAndConfirmedInvitationsArePreserved()
    {
        var (eventDetails, publicId) = await PrepareAsync();
        var confirmed = await SubmitAsync(publicId, 1);
        await DrainAsync();
        var legacy = await SubmitAsync(publicId, 2);
        await using (var db = fixture.CreateDb())
        {
            var row = await db.RegistrationSubmissions.FindAsync(legacy.Registration.Id);
            row!.Status = RegistrationStatus.WAITING_LIST;
            var review = await db.GuestAiReviews.SingleAsync(r => r.RegistrationSubmissionId == row.Id);
            review.Status = AiAnalysisStatus.COMPLETED;
            review.Recommendation = AiRecommendation.ELIGIBLE;
            review.Confidence = 0.9;
            review.Reasons = ["Historical advice only"];
            review.Model = "qwen3:8b";
            review.PromptVersion = "guest-filtering-v1";
            review.AnalyzedAt = fixture.Clock.GetUtcNow();
            await db.SaveChangesAsync();
        }
        await fixture.WithServiceAsync(s => s.SetSeatLimitAsync(eventDetails.Id, "planner", 2, Ct));
        var before = await fixture.WithServiceAsync(s => s.GetRegistrationAsync(eventDetails.Id, "planner", legacy.Registration.Id, Ct));
        Assert.Equal(RegistrationStatus.WAITING_LIST, before.Status);
        fixture.Ai.Decision = Decision(AiDecision.REJECTED);
        await DrainAsync();
        var oldConfirmed = await fixture.WithServiceAsync(s => s.GetRegistrationAsync(eventDetails.Id, "planner", confirmed.Registration.Id, Ct));
        Assert.Equal(RegistrationStatus.CONFIRMED, oldConfirmed.Status);
        Assert.NotNull(oldConfirmed.Invitation);
        Assert.Equal(RegistrationStatus.REJECTED, (await fixture.WithServiceAsync(s => s.GetRegistrationAsync(eventDetails.Id, "planner", legacy.Registration.Id, Ct))).Status);
        await using var check = fixture.CreateDb();
        var audit = await check.GuestAiReviews.SingleAsync(r => r.RegistrationSubmissionId == legacy.Registration.Id);
        Assert.Equal(AiRecommendation.ELIGIBLE, audit.Recommendation);
        Assert.Equal(AiDecision.REJECTED, audit.Decision);
    }

    [Fact]
    public async Task ConcurrentAcceptedDecisionsCannotOverbook()
    {
        var (eventDetails, publicId) = await PrepareAsync(seats: 2);
        for (var i = 0; i < 8; i++) await SubmitAsync(publicId, i);
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => fixture.WithAiServiceAsync(s => s.ProcessNextAsync(Lease, Ct))));
        await DrainAsync();
        await using var db = fixture.CreateDb();
        Assert.Equal(2, await db.RegistrationSubmissions.CountAsync(r => r.EventId == eventDetails.Id && r.Status == RegistrationStatus.CONFIRMED));
        Assert.Equal(6, await db.RegistrationSubmissions.CountAsync(r => r.EventId == eventDetails.Id && r.Status == RegistrationStatus.WAITING_LIST));
        Assert.Equal(2, await db.Invitations.CountAsync(i => i.RegistrationSubmission.EventId == eventDetails.Id));
    }
}
