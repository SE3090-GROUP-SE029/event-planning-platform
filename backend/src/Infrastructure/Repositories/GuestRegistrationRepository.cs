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
        => db.RegistrationSubmissions.Include(e => e.Guest).Include(e => e.Invitation).Include(e => e.Answers).Include(e => e.CheckIn)
            .Include(e => e.RegistrationForm).ThenInclude(e => e.Event);

    public Task<RegistrationSubmission?> FindRegistrationAsync(long id, CancellationToken ct)
        => Registrations.SingleOrDefaultAsync(e => e.Id == id, ct);

    public Task<RegistrationSubmission?> FindPublicRegistrationAsync(string reference, CancellationToken ct)
        => Registrations.SingleOrDefaultAsync(e => e.PublicReference == reference, ct);

    public Task<Guest?> FindGuestByEmailAsync(Guid eventId, string email, CancellationToken ct)
        => db.Guests.SingleOrDefaultAsync(e => e.EventId == eventId && e.NormalizedEmail == email, ct);

    public Task<bool> RegistrationExistsAsync(Guid eventId, string email, CancellationToken ct)
        => db.RegistrationSubmissions.AnyAsync(e => e.EventId == eventId && e.Guest.NormalizedEmail == email, ct);

    public async Task<IReadOnlySet<string>> RegisteredEmailsAsync(
        Guid eventId,
        IReadOnlyCollection<string> normalizedEmails,
        CancellationToken ct)
    {
        if (normalizedEmails.Count == 0) return new HashSet<string>(StringComparer.Ordinal);
        return (await db.RegistrationSubmissions.AsNoTracking()
                .Where(registration => registration.EventId == eventId &&
                    normalizedEmails.Contains(registration.Guest.NormalizedEmail))
                .Select(registration => registration.Guest.NormalizedEmail)
                .ToListAsync(ct))
            .ToHashSet(StringComparer.Ordinal);
    }

    public async Task<IReadOnlyList<Guest>> FindGuestsByEmailsAsync(
        Guid eventId,
        IReadOnlyCollection<string> normalizedEmails,
        CancellationToken ct)
    {
        if (normalizedEmails.Count == 0) return [];
        return await db.Guests
            .Where(guest => guest.EventId == eventId &&
                normalizedEmails.Contains(guest.NormalizedEmail))
            .ToListAsync(ct);
    }

    public Task<int> ConfirmedCountAsync(Guid eventId, CancellationToken ct)
        => db.RegistrationSubmissions.CountAsync(e => e.EventId == eventId && e.Status == RegistrationStatus.CONFIRMED, ct);

    public async Task<IReadOnlyList<RegistrationSubmission>> AcceptedAsync(Guid eventId, CancellationToken ct)
        => await Registrations.Where(e => e.EventId == eventId && e.Status == RegistrationStatus.ACCEPTED)
            .OrderBy(e => e.RegisteredAt).ThenBy(e => e.Id).ToListAsync(ct);

    public async Task<IReadOnlyList<RegistrationSubmission>> WaitingAsync(Guid eventId, CancellationToken ct)
        => await Registrations.Where(e => e.EventId == eventId && e.Status == RegistrationStatus.WAITLISTED)
            .OrderBy(e => e.RegisteredAt).ThenBy(e => e.Id).ToListAsync(ct);

    public Task<Guid?> NextEventNeedingAllocationAsync(DateTimeOffset now, CancellationToken ct)
        => db.RegistrationForms.AsNoTracking()
            .Where(f => f.Status == RegistrationFormStatus.PUBLISHED &&
                (db.RegistrationSubmissions.Any(r => r.EventId == f.EventId && r.Status == RegistrationStatus.ACCEPTED) ||
                 (f.Event.PreferredDate + f.Event.EventDuration > now.UtcDateTime &&
                  f.SeatLimit > db.RegistrationSubmissions.Count(r => r.EventId == f.EventId && r.Status == RegistrationStatus.CONFIRMED) &&
                  db.RegistrationSubmissions.Any(r => r.EventId == f.EventId && r.Status == RegistrationStatus.WAITLISTED))))
            .OrderBy(f => f.CreatedAt)
            .Select(f => (Guid?)f.EventId)
            .FirstOrDefaultAsync(ct);

    public Task<Guid?> NextEventNeedingInvitationAsync(DateTimeOffset now, CancellationToken ct)
        => db.RegistrationForms.AsNoTracking()
            .Where(f => f.Status == RegistrationFormStatus.PUBLISHED &&
                f.Event.PreferredDate + f.Event.EventDuration > now.UtcDateTime &&
                db.RegistrationSubmissions.Any(r => r.EventId == f.EventId &&
                    r.Status == RegistrationStatus.CONFIRMED && r.Invitation == null))
            .OrderBy(f => f.CreatedAt)
            .Select(f => (Guid?)f.EventId)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<long>> ConfirmedWithoutInvitationAsync(Guid eventId, CancellationToken ct)
        => await db.RegistrationSubmissions.AsNoTracking()
            .Where(r => r.EventId == eventId && r.Status == RegistrationStatus.CONFIRMED && r.Invitation == null)
            .OrderBy(r => r.RegisteredAt).ThenBy(r => r.Id).Select(r => r.Id)
            .ToListAsync(ct);

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
            query = query.Where(e => e.Status == RegistrationStatus.WAITLISTED);
        }
        
        if (checkedIn.HasValue)
        {
            if (checkedIn.Value) query = query.Where(e => e.CheckIn != null);
            else query = query.Where(e => e.CheckIn == null);
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
        => db.Invitations
            .Include(e => e.RegistrationSubmission).ThenInclude(e => e.Guest)
            .Include(e => e.RegistrationSubmission).ThenInclude(e => e.Answers)
            .Include(e => e.RegistrationSubmission).ThenInclude(e => e.CheckIn)
            .SingleOrDefaultAsync(e => e.Token == token, ct);

    public void AddForm(RegistrationForm form) => db.RegistrationForms.Add(form);
    public void AddQuestion(RegistrationQuestion question) => db.RegistrationQuestions.Add(question);
    public void AddGuest(Guest guest) => db.Guests.Add(guest);
    public void AddGuests(IEnumerable<Guest> guests) => db.Guests.AddRange(guests);
    public void AddRegistrationLinkEmailJobs(IEnumerable<RegistrationLinkEmailJob> jobs)
        => db.RegistrationLinkEmailJobs.AddRange(jobs);
    public async Task<(Guid EventId, long Id)?> PendingInvitationDeliveryAsync(DateTimeOffset retryBefore, CancellationToken ct)
    {
        var row = await db.RegistrationSubmissions.AsNoTracking().Where(r =>
            r.Status == RegistrationStatus.CONFIRMED && r.Invitation != null && r.Invitation.RevokedAt == null &&
                r.Invitation.TokenExpiresAt > retryBefore.AddMinutes(1) && r.Invitation.DeliveryStatus == InvitationDeliveryStatus.PENDING &&
                (r.Invitation.LastAttemptAt == null || r.Invitation.LastAttemptAt <= retryBefore))
            .OrderBy(r => r.RegisteredAt).ThenBy(r => r.Id).Select(r => new { r.EventId, r.Id }).FirstOrDefaultAsync(ct);
        return row is null ? null : (row.EventId, row.Id);
    }

    public async Task<RegistrationLinkEmailJob?> ClaimNextRegistrationLinkEmailJobAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var queued = RegistrationLinkEmailDeliveryStatus.QUEUED.ToString();
        var processing = RegistrationLinkEmailDeliveryStatus.PROCESSING.ToString();
        var job = (await db.RegistrationLinkEmailJobs
            .FromSqlInterpolated($"""
                SELECT * FROM "RegistrationLinkEmailJobs"
                WHERE (("Status" = {queued} AND "NextAttemptAt" <= {now} AND "AttemptCount" < 5)
                    OR ("Status" = {processing} AND "LockedUntil" <= {now}))
                ORDER BY "CreatedAt"
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(ct))
            .FirstOrDefault();

        if (job is null)
        {
            await transaction.CommitAsync(ct);
            return null;
        }

        if (job.AttemptCount >= 5)
        {
            job.Status = RegistrationLinkEmailDeliveryStatus.FAILED;
            job.LockedUntil = null;
            job.LastFailureCode = "LEASE_EXPIRED";
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return null;
        }

        job.Status = RegistrationLinkEmailDeliveryStatus.PROCESSING;
        job.AttemptCount++;
        job.LockedUntil = now.Add(leaseDuration);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        db.Entry(job).State = EntityState.Detached;
        return job;
    }

    public async Task CompleteRegistrationLinkEmailJobAsync(
        Guid jobId,
        EmailDeliveryResult result,
        DateTimeOffset completedAt,
        CancellationToken ct)
    {
        var job = await db.RegistrationLinkEmailJobs.SingleOrDefaultAsync(item => item.Id == jobId, ct)
            ?? throw new InvalidOperationException($"Registration link email job {jobId} was not found.");

        job.LockedUntil = null;
        if (result == EmailDeliveryResult.SENT)
        {
            job.Status = RegistrationLinkEmailDeliveryStatus.SENT;
            job.SentAt = completedAt;
            job.LastFailureCode = null;
        }
        else if (job.AttemptCount >= 5)
        {
            job.Status = RegistrationLinkEmailDeliveryStatus.FAILED;
            job.LastFailureCode = result.ToString();
        }
        else
        {
            job.Status = RegistrationLinkEmailDeliveryStatus.QUEUED;
            job.NextAttemptAt = completedAt.Add(DeliveryRetryDelay(job.AttemptCount));
            job.LastFailureCode = result.ToString();
        }

        await db.SaveChangesAsync(ct);
    }

    private static TimeSpan DeliveryRetryDelay(int attemptCount)
        => attemptCount switch
        {
            1 => TimeSpan.FromSeconds(15),
            2 => TimeSpan.FromMinutes(1),
            3 => TimeSpan.FromMinutes(5),
            _ => TimeSpan.FromMinutes(15)
        };


    public async Task<(Guid EventId, long Id)?> PendingRejectionDeliveryAsync(DateTimeOffset retryBefore, CancellationToken ct)
    {
        var row = await db.RegistrationSubmissions.AsNoTracking().Where(r =>
            r.Status == RegistrationStatus.REJECTED && r.RejectionDeliveryStatus == InvitationDeliveryStatus.PENDING &&
            (r.RejectionLastAttemptAt == null || r.RejectionLastAttemptAt <= retryBefore))
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
            .Include(r => r.Invitation).Include(r => r.CheckIn)
            .Where(r => r.EventId == eventId)
            .ToListAsync(ct);

        int total = registrations.Count;
        int pendingReview = registrations.Count(r => r.Status == RegistrationStatus.PENDING_REVIEW);
        int accepted = registrations.Count(r => r.ReviewDecision == RegistrationDecision.ACCEPTED);
        int rejected = registrations.Count(r => r.Status == RegistrationStatus.REJECTED);
        int waitlisted = registrations.Count(r => r.Status == RegistrationStatus.WAITLISTED);

        int rsvpAccepted = registrations.Count(r => r.Invitation?.RsvpStatus == RsvpStatus.ACCEPTED);
        int rsvpDeclined = registrations.Count(r => r.Invitation?.RsvpStatus == RsvpStatus.DECLINED);
        int rsvpMaybe = registrations.Count(r => r.Invitation?.RsvpStatus == RsvpStatus.MAYBE);

        int checkedIn = registrations.Count(r => r.CheckIn is not null);
        int notCheckedIn = total - checkedIn;

        var capacity = await db.RegistrationForms.AsNoTracking()
            .Where(form => form.EventId == eventId)
            .Select(form => (int?)form.SeatLimit)
            .SingleOrDefaultAsync(ct) ?? ev.GuestCount;
        var confirmed = registrations.Count(r => r.Status == RegistrationStatus.CONFIRMED);
        int availableSeats = Math.Max(0, capacity - confirmed);

        return new Application.Dtos.Events.EventAnalyticsDto(
            TotalRegistrations: total,
            PendingReview: pendingReview,
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
