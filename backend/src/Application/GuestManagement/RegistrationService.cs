using Domain.Entities;
using Domain.Enums;

namespace Application.GuestManagement;

public class RegistrationService(IGuestRegistrationRepository repository,
    IRegistrationTokenGenerator tokens, TimeProvider clock,
    IRegistrationSecretProtector secretProtector,
    SeatAllocationService allocation)
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
            var result = await client.SuggestAsync(new AiEventContext(form.Event.EventName, form.Event.Requirements), ct);
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
                    Question = question.Question, Required = question.Required ?? false, DisplayOrder = index };
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
        var form = await repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            RequireOwner(eventDetails, plannerId);
            var form = await RequireFormAsync(eventId, ct);
            await RequireCapacityAsync(eventId, settings.SeatLimit, ct);
            form.OpensAt = settings.OpensAt;
            form.ClosesAt = settings.ClosesAt;
            form.SeatLimit = settings.SeatLimit;
            form.UpdatedAt = clock.GetUtcNow();
            return form;
        }, ct);
        await allocation.AllocateAsync(eventId, ct);
        return form;
    }

    public async Task<RegistrationForm> SetSeatLimitAsync(Guid eventId, string plannerId, int seatLimit, CancellationToken ct)
    {
        RegistrationValidator.ValidateSeatLimit(seatLimit);
        var form = await repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            RequireOwner(eventDetails, plannerId);
            var form = await RequireFormAsync(eventId, ct);
            await RequireCapacityAsync(eventId, seatLimit, ct);
            form.SeatLimit = seatLimit;
            form.UpdatedAt = clock.GetUtcNow();
            return form;
        }, ct);
        await allocation.AllocateAsync(eventId, ct);
        return form;
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
            if (now >= form.ClosesAt || now >= new DateTimeOffset(eventDetails.PreferredDate + eventDetails.EventDuration, TimeSpan.Zero))
                throw Error(409, "registration_closed", "Registration has closed.");
            var normalizedEmail = details.EmailAddress.ToUpperInvariant();
            if (await repository.RegistrationExistsAsync(eventDetails.Id, normalizedEmail, ct))
                throw Error(409, "duplicate_registration", "This email is already registered for this event.");
            var guest = await repository.FindGuestByEmailAsync(eventDetails.Id, normalizedEmail, ct);
            if (guest is null)
            {
                guest = new Guest
                {
                    EventId = eventDetails.Id, FullName = details.FullName, EmailAddress = details.EmailAddress,
                    NormalizedEmail = normalizedEmail, Organisation = details.Organisation, PhoneNumber = details.PhoneNumber,
                    CreatedAt = now, UpdatedAt = now
                };
                repository.AddGuest(guest);
            }
            else
            {
                guest.FullName = details.FullName;
                guest.EmailAddress = details.EmailAddress;
                guest.Organisation = details.Organisation;
                guest.PhoneNumber = details.PhoneNumber;
                guest.UpdatedAt = now;
            }
            var submission = new RegistrationSubmission
            {
                EventId = eventDetails.Id, RegistrationFormId = form.Id, RegistrationForm = form,
                GuestId = guest.Id, Guest = guest, PublicReference = await UniqueTokenAsync(ct),
                StatusSecretHash = tokens.Hash(secret), ProtectedStatusSecret = secretProtector.Protect(secret),
                RegisteredAt = now, UpdatedAt = now,
                Status = RegistrationStatus.PENDING_REVIEW,
                Answers = validatedAnswers.Select(a => new RegistrationAnswer { RegistrationQuestionId = a.QuestionId,
                    RegistrationFormId = form.Id, Answer = a.Answer }).ToList()
            };
            repository.AddRegistration(submission);
            await repository.SaveAsync(ct);
            return submission;
        }, ct);
        return new RegistrationReceipt(result, secret);
    }

    public Task<RegistrationPage> ListAsync(Guid eventId, string plannerId, int page, int pageSize, CancellationToken ct, RegistrationStatus? status = null, RsvpStatus? rsvpStatus = null, bool? isWaitlisted = null, bool? checkedIn = null)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            throw Error(400, "invalid_pagination", "Page must be positive and page size must be between 1 and 100.");
        return repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            RequireOwner(eventDetails, plannerId);
            await RequireFormAsync(eventId, ct);
            return await repository.ListAsync(eventId, page, pageSize, ct, status, rsvpStatus, isWaitlisted, checkedIn);
        }, ct);
    }

    public Task<RegistrationSubmission> GetRegistrationAsync(Guid eventId, string plannerId, long id, CancellationToken ct)
        => repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            RequireOwner(eventDetails, plannerId);
            return await RequireRegistrationAsync(eventId, id, ct);
        }, ct);

    public async Task<RegistrationSubmission> GetPublicStatusAsync(string reference, string secret, CancellationToken ct)
        => await RegistrationAccess.GetPublicRegistrationAsync(repository, tokens, reference, secret, ct);

    public async Task<RegistrationSubmission> CancelAsync(Guid eventId, string plannerId, long id, CancellationToken ct)
    {
        var registration = await repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            RequireOwner(eventDetails, plannerId);
            var registration = await RequireRegistrationAsync(eventId, id, ct);
            RegistrationTransitions.Cancel(registration, clock);
            return registration;
        }, ct);
        await allocation.AllocateAsync(eventId, ct);
        return registration;
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
        => RegistrationAccess.RequireOwner(eventDetails, plannerId);

    private static RegistrationException NotFound() => Error(404, "not_found", "Registration resource not found.");
    private static RegistrationException Error(int status, string code, string message) => new(status, code, message);
}
