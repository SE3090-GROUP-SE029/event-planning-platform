using Domain.Entities;
using Domain.Enums;

namespace Application.GuestManagement;

public class RegistrationService(IGuestRegistrationRepository repository,
    IRegistrationTokenGenerator tokens, IRegistrationEligibilityPolicy eligibility,
    IInvitationEmailSender emailSender, TimeProvider clock)
{
    public Task<RegistrationForm> CreateFormAsync(Guid eventId, string plannerId, FormSettings settings, CancellationToken ct)
    {
        settings = RegistrationValidator.Validate(settings);
        return repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            RequireOwner(eventDetails, plannerId);
            if (await repository.FindFormAsync(eventId, ct) is not null)
                throw Error(409, "form_exists", "This event already has a registration form.");
            var now = clock.GetUtcNow();
            var form = new RegistrationForm
            {
                EventId = eventId, Event = eventDetails, OpensAt = settings.OpensAt,
                ClosesAt = settings.ClosesAt, SeatLimit = settings.SeatLimit, CreatedAt = now, UpdatedAt = now
            };
            repository.AddForm(form);
            return form;
        }, ct);
    }

    public Task<RegistrationForm> GetFormAsync(Guid eventId, string plannerId, CancellationToken ct)
        => repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            RequireOwner(eventDetails, plannerId);
            return await RequireFormAsync(eventId, ct);
        }, ct);

    public async Task<QuestionSuggestions> SuggestQuestionsAsync(Guid eventId, string plannerId,
        IRegistrationQuestionClient client, CancellationToken ct)
    {
        var form = await GetFormAsync(eventId, plannerId, ct);
        if (form.Status != RegistrationFormStatus.DRAFT) throw Error(409, "form_published", "Published questions cannot be changed.");
        try
        {
            var result = await client.SuggestAsync(new AiEventContext(form.Event.EventName, form.Event.RequirementNotes), ct);
            return new QuestionSuggestions(RegistrationValidator.ValidateQuestions(result.Questions));
        }
        catch (AiAnalysisException error)
        {
            throw Error(503, error.Code, "Question suggestions are unavailable. Please retry.");
        }
    }

    public Task<RegistrationForm> SelectQuestionsAsync(Guid eventId, string plannerId,
        IReadOnlyList<QuestionSelection>? questions, CancellationToken ct)
    {
        var selected = RegistrationValidator.ValidateQuestions(questions);
        return repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            RequireOwner(eventDetails, plannerId);
            var form = await RequireFormAsync(eventId, ct);
            if (form.Status != RegistrationFormStatus.DRAFT) throw Error(409, "form_published", "Published questions cannot be changed.");
            foreach (var old in form.Questions) old.IsSelected = false;
            await repository.SaveAsync(ct); // Release selected-order slots before inserting replacement selections.
            foreach (var (question, index) in selected.Select((q, i) => (q, i)))
            {
                var saved = new RegistrationQuestion { RegistrationFormId = form.Id,
                    Question = question.Question, Required = question.Required, DisplayOrder = index };
                form.Questions.Add(saved);
                repository.AddQuestion(saved);
            }
            form.UpdatedAt = clock.GetUtcNow();
            return form;
        }, ct);
    }

    public async Task<RegistrationForm> UpdateFormAsync(Guid eventId, string plannerId, FormSettings settings, CancellationToken ct)
    {
        settings = RegistrationValidator.Validate(settings);
        var result = await repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            RequireOwner(eventDetails, plannerId);
            var form = await RequireFormAsync(eventId, ct);
            await RequireCapacityAsync(eventId, settings.SeatLimit, ct);
            form.OpensAt = settings.OpensAt;
            form.ClosesAt = settings.ClosesAt;
            form.SeatLimit = settings.SeatLimit;
            form.UpdatedAt = clock.GetUtcNow();
            return (Form: form, Promoted: await PromoteAsync(form, eventDetails, ct));
        }, ct);
        await DeliverAsync(eventId, result.Promoted, ct);
        return result.Form;
    }

    public async Task<RegistrationForm> SetSeatLimitAsync(Guid eventId, string plannerId, int seatLimit, CancellationToken ct)
    {
        RegistrationValidator.ValidateSeatLimit(seatLimit);
        var result = await repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            RequireOwner(eventDetails, plannerId);
            var form = await RequireFormAsync(eventId, ct);
            await RequireCapacityAsync(eventId, seatLimit, ct);
            form.SeatLimit = seatLimit;
            form.UpdatedAt = clock.GetUtcNow();
            return (Form: form, Promoted: await PromoteAsync(form, eventDetails, ct));
        }, ct);
        await DeliverAsync(eventId, result.Promoted, ct);
        return result.Form;
    }

    public Task<RegistrationForm> PublishAsync(Guid eventId, string plannerId, CancellationToken ct)
        => repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            RequireOwner(eventDetails, plannerId);
            var form = await RequireFormAsync(eventId, ct);
            if (form.Status == RegistrationFormStatus.PUBLISHED) return form;
            if (clock.GetUtcNow() >= form.ClosesAt)
                throw Error(409, "registration_closed", "A registration form that has closed cannot be published.");
            form.PublicId = await UniqueTokenAsync(ct);
            form.Status = RegistrationFormStatus.PUBLISHED;
            form.PublishedAt = form.UpdatedAt = clock.GetUtcNow();
            return form;
        }, ct);

    public async Task<RegistrationForm> GetPublicFormAsync(string publicId, CancellationToken ct)
    {
        RegistrationValidator.ValidatePublicCredential(publicId);
        return await repository.FindPublicFormAsync(publicId, ct) ?? throw NotFound();
    }

    public Task<RegistrationReceipt> SubmitAsync(string publicId, GuestDetails details, CancellationToken ct)
        => SubmitAsync(publicId, details, null, ct);

    public async Task<RegistrationReceipt> SubmitAsync(string publicId, GuestDetails details,
        IReadOnlyList<AnswerSubmission>? answers, CancellationToken ct)
    {
        details = RegistrationValidator.Validate(details);
        var lookup = await GetPublicFormAsync(publicId, ct);
        var secret = tokens.Generate();
        var result = await repository.WithEventLockAsync(lookup.EventId, async eventDetails =>
        {
            // Re-read after obtaining the lock; the form may have changed while this request waited.
            var form = await RequireFormAsync(eventDetails.Id, ct);
            if (form.Status != RegistrationFormStatus.PUBLISHED || form.PublicId != publicId) throw NotFound();
            var validatedAnswers = RegistrationValidator.ValidateAnswers(form, answers);
            var now = clock.GetUtcNow();
            if (now < form.OpensAt) throw Error(409, "registration_not_open", "Registration has not opened yet.");
            if (now >= form.ClosesAt || now >= eventDetails.EventEndDate)
                throw Error(409, "registration_closed", "Registration has closed.");
            var normalizedEmail = details.EmailAddress.ToUpperInvariant();
            if (await repository.EmailExistsAsync(eventDetails.Id, normalizedEmail, ct))
                throw Error(409, "duplicate_registration", "This email is already registered for this event.");
            var guest = new Guest
            {
                EventId = eventDetails.Id, FullName = details.FullName, EmailAddress = details.EmailAddress,
                NormalizedEmail = normalizedEmail, Organisation = details.Organisation, PhoneNumber = details.PhoneNumber,
                CreatedAt = now, UpdatedAt = now
            };
            var submission = new RegistrationSubmission
            {
                EventId = eventDetails.Id, RegistrationFormId = form.Id, RegistrationForm = form,
                GuestId = guest.Id, Guest = guest, PublicReference = await UniqueTokenAsync(ct),
                StatusSecretHash = tokens.Hash(secret), RegisteredAt = now, UpdatedAt = now,
                Status = RegistrationStatus.PENDING_AI,
                Answers = validatedAnswers.Select(a => new RegistrationAnswer { RegistrationQuestionId = a.QuestionId,
                    RegistrationFormId = form.Id, Answer = a.Answer }).ToList()
            };
            repository.AddRegistration(submission);
            await repository.SaveAsync(ct);
            return submission;
        }, ct);
        return new RegistrationReceipt(result, secret);
    }

    public async Task ApplyDecisionAsync(GuestAiClaim claim, GuestAiDecision decision,
        IGuestAiReviewRepository reviews, CancellationToken ct)
    {
        var lookup = await repository.FindRegistrationAsync(claim.RegistrationSubmissionId, ct) ?? throw NotFound();
        var promoted = await repository.WithEventLockAsync(lookup.EventId, async eventDetails =>
        {
            var registration = await RequireRegistrationAsync(eventDetails.Id, lookup.Id, ct);
            if (!await reviews.CompleteAsync(claim, decision, clock.GetUtcNow(), ct)) return new List<long>();
            // An in-flight result never changes a cancellation or a legacy confirmed invitation.
            if (registration.Status is not (RegistrationStatus.PENDING_AI or RegistrationStatus.WAITING_LIST)) return new List<long>();
            registration.UpdatedAt = clock.GetUtcNow();
            if (decision.Decision == AiDecision.REJECTED)
            {
                registration.Status = RegistrationStatus.REJECTED;
                registration.RejectionDeliveryStatus = InvitationDeliveryStatus.PENDING;
                return new List<long>();
            }
            registration.Status = RegistrationStatus.WAITING_LIST;
            await repository.SaveAsync(ct);
            return await PromoteAsync(await RequireFormAsync(eventDetails.Id, ct), eventDetails, ct);
        }, ct);
        await DeliverAsync(lookup.EventId, promoted, ct);
        await DeliverRejectionAsync(lookup.EventId, lookup.Id, ct);
    }

    public async Task<bool> ProcessPendingDeliveryAsync(CancellationToken ct)
    {
        var pending = await repository.PendingDeliveryAsync(clock.GetUtcNow().AddMinutes(-1), ct);
        if (pending is null) return false;
        await DeliverAsync(pending.Value.EventId, [pending.Value.Id], ct);
        await DeliverRejectionAsync(pending.Value.EventId, pending.Value.Id, ct);
        return true;
    }

    public async Task<RegistrationSubmission> RetryRejectionAsync(Guid eventId, string plannerId, long id, CancellationToken ct)
    {
        var registration = await GetRegistrationAsync(eventId, plannerId, id, ct);
        if (registration.Status != RegistrationStatus.REJECTED)
            throw Error(409, "rejection_unavailable", "Only a rejected registration can receive a rejection email.");
        await DeliverRejectionAsync(eventId, id, ct, retry: true);
        return await GetRegistrationAsync(eventId, plannerId, id, ct);
    }

    private Task<bool> DeliverRejectionAsync(Guid eventId, long id, CancellationToken ct, bool retry = false)
        => repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            var registration = await RequireRegistrationAsync(eventId, id, ct);
            if (registration.Status != RegistrationStatus.REJECTED || registration.RejectionDeliveryStatus == InvitationDeliveryStatus.SENT ||
                (!retry && (registration.RejectionDeliveryStatus == InvitationDeliveryStatus.FAILED ||
                    registration.RejectionLastAttemptAt > clock.GetUtcNow().AddMinutes(-1)))) return false;
            registration.RejectionLastAttemptAt = clock.GetUtcNow();
            registration.RejectionDeliveryAttempts++;
            EmailDeliveryResult delivery;
            try { delivery = await emailSender.SendRejectionAsync(new RejectionEmail(registration.Guest.EmailAddress,
                registration.Guest.FullName, eventDetails.EventName), ct); }
            catch (Exception) when (!ct.IsCancellationRequested) { delivery = EmailDeliveryResult.FAILED; }
            registration.RejectionDeliveryStatus = delivery switch
            {
                EmailDeliveryResult.SENT => InvitationDeliveryStatus.SENT,
                EmailDeliveryResult.UNAVAILABLE => InvitationDeliveryStatus.PENDING,
                _ => InvitationDeliveryStatus.FAILED
            };
            if (delivery == EmailDeliveryResult.SENT) registration.RejectionSentAt = clock.GetUtcNow();
            return true;
        }, ct);

    public Task<RegistrationPage> ListAsync(Guid eventId, string plannerId, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            throw Error(400, "invalid_pagination", "Page must be positive and page size must be between 1 and 100.");
        return repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            RequireOwner(eventDetails, plannerId);
            await RequireFormAsync(eventId, ct);
            return await repository.ListAsync(eventId, page, pageSize, ct);
        }, ct);
    }

    public Task<RegistrationSubmission> GetRegistrationAsync(Guid eventId, string plannerId, long id, CancellationToken ct)
        => repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            RequireOwner(eventDetails, plannerId);
            return await RequireRegistrationAsync(eventId, id, ct);
        }, ct);

    public async Task<RegistrationSubmission> GetPublicStatusAsync(string reference, string secret, CancellationToken ct)
    {
        RegistrationValidator.ValidatePublicCredential(reference);
        RegistrationValidator.ValidatePublicCredential(secret);
        var registration = await repository.FindPublicRegistrationAsync(reference, ct);
        if (registration is null || !tokens.Matches(secret, registration.StatusSecretHash)) throw NotFound();
        return registration;
    }

    public async Task<RegistrationSubmission> CancelAsync(Guid eventId, string plannerId, long id, CancellationToken ct)
    {
        var result = await repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            RequireOwner(eventDetails, plannerId);
            var registration = await RequireRegistrationAsync(eventId, id, ct);
            Cancel(registration);
            await repository.SaveAsync(ct);
            return (Registration: registration, Promoted: await PromoteAsync(await RequireFormAsync(eventId, ct), eventDetails, ct));
        }, ct);
        await DeliverAsync(eventId, result.Promoted, ct);
        return result.Registration;
    }

    public async Task<RegistrationSubmission> RespondAsync(string reference, string secret, RsvpStatus response, CancellationToken ct)
    {
        if (response is not (RsvpStatus.ACCEPTED or RsvpStatus.DECLINED or RsvpStatus.MAYBE))
            throw Error(400, "invalid_rsvp", "RSVP must be ACCEPTED, DECLINED, or MAYBE.");
        var lookup = await GetPublicStatusAsync(reference, secret, ct);
        var result = await repository.WithEventLockAsync(lookup.EventId, async eventDetails =>
        {
            var registration = await GetPublicStatusAsync(reference, secret, ct);
            if (response == RsvpStatus.DECLINED)
            {
                Cancel(registration);
                await repository.SaveAsync(ct);
                return (Registration: registration, Promoted: await PromoteAsync(await RequireFormAsync(lookup.EventId, ct), eventDetails, ct));
            }
            if (!IsActive(registration))
                throw Error(409, "invitation_unavailable", "Only an active confirmed invitation can accept an RSVP.");
            registration.Invitation!.RsvpStatus = response;
            registration.Invitation.RsvpedAt = registration.UpdatedAt = clock.GetUtcNow();
            return (Registration: registration, Promoted: new List<long>());
        }, ct);
        await DeliverAsync(lookup.EventId, result.Promoted, ct);
        return result.Registration;
    }

    public async Task<RegistrationSubmission> RetryInvitationAsync(Guid eventId, string plannerId, long id, CancellationToken ct)
    {
        var registration = await GetRegistrationAsync(eventId, plannerId, id, ct);
        if (!IsActive(registration)) throw Error(409, "invitation_unavailable", "No active confirmed invitation exists.");
        await DeliverAsync(eventId, [id], ct);
        return await GetRegistrationAsync(eventId, plannerId, id, ct);
    }

    public Task<bool> ValidateInvitationAsync(Guid eventId, string plannerId, string token, CancellationToken ct)
        => repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            RequireOwner(eventDetails, plannerId);
            RegistrationValidator.ValidatePublicCredential(token);
            var invitation = await repository.FindInvitationByTokenAsync(token, ct);
            return invitation is not null && invitation.RegistrationSubmission.EventId == eventId && IsActive(invitation.RegistrationSubmission);
        }, ct);

    public bool IsActive(RegistrationSubmission registration)
        => registration.Status == RegistrationStatus.CONFIRMED && registration.Invitation is { RevokedAt: null } invitation &&
            clock.GetUtcNow() < invitation.TokenExpiresAt;

    private async Task<List<long>> PromoteAsync(RegistrationForm form, Event eventDetails, CancellationToken ct)
    {
        var promoted = new List<long>();
        if (form.Status != RegistrationFormStatus.PUBLISHED || clock.GetUtcNow() >= eventDetails.EventEndDate) return promoted;
        var available = form.SeatLimit - await repository.ConfirmedCountAsync(form.EventId, ct);
        if (available <= 0) return promoted;
        foreach (var registration in await repository.WaitingAsync(form.EventId, ct))
        {
            if (!await eligibility.IsEligibleAsync(registration.Guest, eventDetails, ct)) continue;
            var now = clock.GetUtcNow();
            registration.Status = RegistrationStatus.CONFIRMED;
            registration.ConfirmedAt = registration.UpdatedAt = now;
            var invitation = new Invitation
            {
                RegistrationSubmissionId = registration.Id, RegistrationSubmission = registration,
                Token = await UniqueTokenAsync(ct), CreatedAt = now, TokenExpiresAt = eventDetails.EventEndDate
            };
            registration.Invitation = invitation;
            repository.AddInvitation(invitation);
            await repository.SaveAsync(ct);
            promoted.Add(registration.Id);
            if (--available == 0) break;
        }
        return promoted;
    }

    private void Cancel(RegistrationSubmission registration)
    {
        if (registration.Status == RegistrationStatus.REJECTED)
            throw Error(409, "registration_rejected", "A rejected registration cannot be cancelled or respond to an invitation.");
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

    private async Task DeliverAsync(Guid eventId, IEnumerable<long> registrations, CancellationToken ct)
    {
        foreach (var id in registrations)
        {
            // Registration is already committed. An interrupted request leaves durable PENDING delivery for retry.
            if (ct.IsCancellationRequested) return;
            await repository.WithEventLockAsync(eventId, async eventDetails =>
            {
                var registration = await RequireRegistrationAsync(eventId, id, ct);
                if (!IsActive(registration) || registration.Invitation!.DeliveryStatus == InvitationDeliveryStatus.SENT) return false;
                var invitation = registration.Invitation!;
                invitation.LastAttemptAt = clock.GetUtcNow();
                invitation.DeliveryAttempts++;
                EmailDeliveryResult delivery;
                try
                {
                    delivery = await emailSender.SendAsync(new InvitationEmail(registration.Guest.EmailAddress,
                        registration.Guest.FullName, eventDetails.EventName, eventDetails.EventStartDate,
                        eventDetails.PreferredLocation, invitation.Token, tokens.CreateQrPng(invitation.Token)), ct);
                }
                catch (Exception) when (!ct.IsCancellationRequested)
                {
                    // Transport details may contain credentials or PII; persist only the delivery outcome.
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

    private async Task<string> UniqueTokenAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var token = tokens.Generate();
            if (!await repository.TokenExistsAsync(token, ct)) return token;
        }
        throw Error(503, "token_generation_failed", "Unable to create a unique registration reference. Please retry.");
    }

    private async Task RequireCapacityAsync(Guid eventId, int capacity, CancellationToken ct)
    {
        if (capacity < await repository.ConfirmedCountAsync(eventId, ct))
            throw Error(409, "capacity_below_confirmed", "Seat limit cannot be below the number of confirmed guests.");
    }

    private async Task<RegistrationForm> RequireFormAsync(Guid eventId, CancellationToken ct)
        => await repository.FindFormAsync(eventId, ct) ?? throw NotFound();

    private async Task<RegistrationSubmission> RequireRegistrationAsync(Guid eventId, long id, CancellationToken ct)
    {
        var registration = await repository.FindRegistrationAsync(id, ct);
        return registration is not null && registration.EventId == eventId ? registration : throw NotFound();
    }

    private static void RequireOwner(Event eventDetails, string plannerId)
    {
        if (string.IsNullOrWhiteSpace(plannerId) || eventDetails.CreatedByUserId != plannerId) throw NotFound();
    }

    private static RegistrationException NotFound() => Error(404, "not_found", "Registration resource not found.");
    private static RegistrationException Error(int status, string code, string message) => new(status, code, message);
}
