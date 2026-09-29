using Application.GuestManagement;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Repositories;

public class GuestRegistrationRepository(AppDbContext db) : IGuestRegistrationRepository
{
    public async Task<T> WithEventLockAsync<T>(Guid eventId, Func<Event, Task<T>> action, CancellationToken ct)
    {
        // Discard pre-lock lookup snapshots. All state used for a decision is read under this lock.
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var eventDetails = await db.Events.FromSqlInterpolated(
                $"SELECT * FROM \"Events\" WHERE \"Id\" = {eventId} FOR UPDATE").SingleOrDefaultAsync(ct)
                ?? throw new RegistrationException(404, "not_found", "Registration resource not found.");
            var result = await action(eventDetails);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new RegistrationException(409, "registration_conflict", "A registration or reference already exists. Check your submission before retrying.");
        }
        finally
        {
            db.ChangeTracker.Clear();
        }
    }

    public Task<RegistrationForm?> FindFormAsync(Guid eventId, CancellationToken ct)
        => db.RegistrationForms.Include(e => e.Event).Include(e => e.Questions).SingleOrDefaultAsync(e => e.EventId == eventId, ct);

    public Task<RegistrationForm?> FindPublicFormAsync(string publicId, CancellationToken ct)
        => db.RegistrationForms.AsNoTracking().Include(e => e.Event).Include(e => e.Questions.Where(q => q.IsSelected))
            .SingleOrDefaultAsync(e => e.PublicId == publicId && e.Status == RegistrationFormStatus.PUBLISHED, ct);

    private IQueryable<RegistrationSubmission> Registrations
        => db.RegistrationSubmissions.Include(e => e.Guest).Include(e => e.Invitation).Include(e => e.Answers)
            .Include(e => e.RegistrationForm).ThenInclude(e => e.Event);

    public Task<RegistrationSubmission?> FindRegistrationAsync(long id, CancellationToken ct)
        => Registrations.SingleOrDefaultAsync(e => e.Id == id, ct);

    public Task<RegistrationSubmission?> FindPublicRegistrationAsync(string reference, CancellationToken ct)
        => Registrations.SingleOrDefaultAsync(e => e.PublicReference == reference, ct);

    public Task<bool> EmailExistsAsync(Guid eventId, string email, CancellationToken ct)
        => db.Guests.AnyAsync(e => e.EventId == eventId && e.NormalizedEmail == email, ct);

    public Task<int> ConfirmedCountAsync(Guid eventId, CancellationToken ct)
        => db.RegistrationSubmissions.CountAsync(e => e.EventId == eventId && e.Status == RegistrationStatus.CONFIRMED, ct);

    public async Task<IReadOnlyList<RegistrationSubmission>> WaitingAsync(Guid eventId, CancellationToken ct)
        => await Registrations.Where(e => e.EventId == eventId && e.Status == RegistrationStatus.WAITING_LIST &&
            db.GuestAiReviews.Any(r => r.RegistrationSubmissionId == e.Id && r.Status == AiAnalysisStatus.COMPLETED && r.Decision == AiDecision.ACCEPTED))
            .OrderBy(e => e.RegisteredAt).ThenBy(e => e.Id).ToListAsync(ct);

    public async Task<RegistrationPage> ListAsync(Guid eventId, int page, int pageSize, CancellationToken ct, RegistrationStatus? status = null, RsvpStatus? rsvpStatus = null, bool? isWaitlisted = null, bool? checkedIn = null)
    {
        var query = Registrations.Where(e => e.EventId == eventId);
        
        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }
        
        if (rsvpStatus.HasValue)
        {
            query = query.Where(e => e.Invitation != null && e.Invitation.RsvpStatus == rsvpStatus.Value);
        }

        if (isWaitlisted.HasValue && isWaitlisted.Value)
        {
            query = query.Where(e => e.Status == RegistrationStatus.WAITING_LIST);
        }
        
        if (checkedIn.HasValue)
        {
            if (checkedIn.Value) query = query.Where(e => e.CheckedInAt != null);
            else query = query.Where(e => e.CheckedInAt == null);
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(e => e.RegisteredAt).ThenBy(e => e.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new RegistrationPage(rows, total, page, pageSize);
    }

    public async Task<bool> TokenExistsAsync(string value, CancellationToken ct)
        => await db.Invitations.AnyAsync(e => e.Token == value, ct) ||
            await db.RegistrationForms.AnyAsync(e => e.PublicId == value, ct) ||
            await db.RegistrationSubmissions.AnyAsync(e => e.PublicReference == value, ct);

    public Task<Invitation?> FindInvitationByTokenAsync(string token, CancellationToken ct)
        => db.Invitations.Include(e => e.RegistrationSubmission).SingleOrDefaultAsync(e => e.Token == token, ct);

    public void AddForm(RegistrationForm form) => db.RegistrationForms.Add(form);
    public void AddQuestion(RegistrationQuestion question) => db.RegistrationQuestions.Add(question);
    public async Task<(Guid EventId, long Id)?> PendingDeliveryAsync(DateTimeOffset retryBefore, CancellationToken ct)
    {
        var row = await db.RegistrationSubmissions.AsNoTracking().Where(r =>
            (r.Status == RegistrationStatus.REJECTED && r.RejectionDeliveryStatus == InvitationDeliveryStatus.PENDING &&
                (r.RejectionLastAttemptAt == null || r.RejectionLastAttemptAt <= retryBefore)) ||
            (r.Status == RegistrationStatus.CONFIRMED && r.Invitation != null && r.Invitation.RevokedAt == null &&
                r.Invitation.TokenExpiresAt > retryBefore.AddMinutes(1) && r.Invitation.DeliveryStatus == InvitationDeliveryStatus.PENDING &&
                (r.Invitation.LastAttemptAt == null || r.Invitation.LastAttemptAt <= retryBefore)))
            .OrderBy(r => r.RegisteredAt).ThenBy(r => r.Id).Select(r => new { r.EventId, r.Id }).FirstOrDefaultAsync(ct);
        return row is null ? null : (row.EventId, row.Id);
    }
    public void AddRegistration(RegistrationSubmission registration)
    {
        db.RegistrationSubmissions.Add(registration);
        // Durable work is committed atomically with the submission; no model call occurs under this lock.
        db.GuestAiReviews.Add(new GuestAiReview
        {
            RegistrationSubmission = registration, RequestedAt = registration.RegisteredAt
        });
    }
    public void AddInvitation(Invitation invitation) => db.Invitations.Add(invitation);
    
    public async Task<Application.Dtos.Events.EventAnalyticsDto> GetEventAnalyticsAsync(Guid eventId, CancellationToken ct)
    {
        var ev = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId, ct)
                 ?? throw new KeyNotFoundException("Event not found");

        var registrations = await db.RegistrationSubmissions
            .AsNoTracking()
            .Include(r => r.Invitation)
            .Where(r => r.EventId == eventId)
            .ToListAsync(ct);

        int total = registrations.Count;
        int pendingAi = registrations.Count(r => r.Status == RegistrationStatus.PENDING_AI);
        int accepted = registrations.Count(r => r.Status == RegistrationStatus.CONFIRMED);
        int rejected = registrations.Count(r => r.Status == RegistrationStatus.REJECTED);
        int waitlisted = registrations.Count(r => r.Status == RegistrationStatus.WAITING_LIST);

        int rsvpAccepted = registrations.Count(r => r.Invitation?.RsvpStatus == RsvpStatus.ACCEPTED);
        int rsvpDeclined = registrations.Count(r => r.Invitation?.RsvpStatus == RsvpStatus.DECLINED);
        int rsvpMaybe = registrations.Count(r => r.Invitation?.RsvpStatus == RsvpStatus.MAYBE);

        int checkedIn = registrations.Count(r => r.CheckedInAt.HasValue);
        int notCheckedIn = total - checkedIn;

        int capacity = ev.GuestCount;
        int availableSeats = Math.Max(0, capacity - accepted);

        return new Application.Dtos.Events.EventAnalyticsDto(
            TotalRegistrations: total,
            PendingAi: pendingAi,
            Accepted: accepted,
            Rejected: rejected,
            Waitlisted: waitlisted,
            RsvpAccepted: rsvpAccepted,
            RsvpDeclined: rsvpDeclined,
            RsvpMaybe: rsvpMaybe,
            CheckedIn: checkedIn,
            NotCheckedIn: notCheckedIn,
            AvailableSeats: availableSeats,
            Capacity: capacity
        );
    }

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
