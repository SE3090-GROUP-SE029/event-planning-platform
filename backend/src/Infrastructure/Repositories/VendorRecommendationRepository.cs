using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Repositories;

public class VendorRecommendationRepository : IVendorRecommendationRepository
{
    private readonly AppDbContext _db;

    public VendorRecommendationRepository(AppDbContext db) => _db = db;

    public async Task AddRunAsync(VendorRecommendationRun run, CancellationToken cancellationToken = default) =>
        await _db.VendorRecommendationRuns.AddAsync(run, cancellationToken);

    public async Task<bool> TryAddRunAsync(
        VendorRecommendationRun run,
        CancellationToken cancellationToken = default)
    {
        await _db.VendorRecommendationRuns.AddAsync(run, cancellationToken);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_VendorRecommendationRuns_EventId"
            })
        {
            _db.Entry(run).State = EntityState.Detached;
            if (await GetActiveForEventAsync(run.EventId, cancellationToken) is null)
                throw;
            return false;
        }
    }

    public Task<VendorRecommendationRun?> GetLatestForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default) =>
        _db.VendorRecommendationRuns
            .AsNoTracking()
            .Include(r => r.Items)
            .Where(r => r.EventId == eventId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<VendorRecommendationRun?> GetActiveForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default) =>
        _db.VendorRecommendationRuns
            .AsNoTracking()
            .Include(r => r.Items)
            .Where(r => r.EventId == eventId &&
                        (r.Status == VendorRecommendationRun.PendingStatus ||
                         r.Status == VendorRecommendationRun.RunningStatus))
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<VendorRecommendationRun?> GetByIdAsync(
        Guid runId,
        CancellationToken cancellationToken = default) =>
        _db.VendorRecommendationRuns
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);

    public Task<VendorRecommendationRun?> GetNextRunnableAsync(
        DateTime staleRunningBefore,
        CancellationToken cancellationToken = default) =>
        _db.VendorRecommendationRuns
            .AsNoTracking()
            .Where(r => r.Status == VendorRecommendationRun.PendingStatus ||
                        (r.Status == VendorRecommendationRun.RunningStatus &&
                         r.UpdatedAt <= staleRunningBefore))
            .OrderBy(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> TryClaimAsync(
        Guid runId,
        DateTime now,
        DateTime staleRunningBefore,
        CancellationToken cancellationToken = default)
    {
        if (!_db.Database.IsRelational())
        {
            var run = await _db.VendorRecommendationRuns
                .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);
            if (run is null ||
                (run.Status != VendorRecommendationRun.PendingStatus &&
                 (run.Status != VendorRecommendationRun.RunningStatus ||
                  run.UpdatedAt > staleRunningBefore)))
                return false;

            run.Status = VendorRecommendationRun.RunningStatus;
            run.Stage = "Preparing recommendations";
            run.StartedAt = now;
            run.UpdatedAt = now;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        var claimed = await _db.VendorRecommendationRuns
            .Where(r => r.Id == runId &&
                        (r.Status == VendorRecommendationRun.PendingStatus ||
                         (r.Status == VendorRecommendationRun.RunningStatus &&
                          r.UpdatedAt <= staleRunningBefore)))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.Status, VendorRecommendationRun.RunningStatus)
                    .SetProperty(r => r.Stage, "Preparing recommendations")
                    .SetProperty(r => r.StartedAt, now)
                    .SetProperty(r => r.UpdatedAt, now),
                cancellationToken);
        return claimed == 1;
    }

    public async Task<bool> UpdateProgressAsync(
        Guid runId,
        string stage,
        int? candidateCount,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        if (!_db.Database.IsRelational())
        {
            var run = await _db.VendorRecommendationRuns
                .FirstOrDefaultAsync(
                    r => r.Id == runId &&
                         r.Status == VendorRecommendationRun.RunningStatus,
                    cancellationToken);
            if (run is null)
                return false;

            run.Stage = stage;
            if (candidateCount.HasValue)
                run.CandidateCount = candidateCount.Value;
            run.UpdatedAt = updatedAt;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        var query = _db.VendorRecommendationRuns
            .Where(r => r.Id == runId && r.Status == VendorRecommendationRun.RunningStatus);
        var updated = candidateCount.HasValue
            ? await query.ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.Stage, stage)
                    .SetProperty(r => r.CandidateCount, candidateCount.Value)
                    .SetProperty(r => r.UpdatedAt, updatedAt),
                cancellationToken)
            : await query.ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.Stage, stage)
                    .SetProperty(r => r.UpdatedAt, updatedAt),
                cancellationToken);
        return updated == 1;
    }

    public async Task<bool> MarkFailedIfRunningAsync(
        Guid runId,
        string failureMessage,
        DateTime completedAt,
        CancellationToken cancellationToken = default)
    {
        if (!_db.Database.IsRelational())
        {
            var run = await _db.VendorRecommendationRuns
                .FirstOrDefaultAsync(
                    r => r.Id == runId &&
                         r.Status == VendorRecommendationRun.RunningStatus,
                    cancellationToken);
            if (run is null)
                return false;

            run.Status = VendorRecommendationRun.FailedStatus;
            run.FailureMessage = failureMessage;
            run.CompletedAt = completedAt;
            run.UpdatedAt = completedAt;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        var failed = await _db.VendorRecommendationRuns
            .Where(r => r.Id == runId && r.Status == VendorRecommendationRun.RunningStatus)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.Status, VendorRecommendationRun.FailedStatus)
                    .SetProperty(r => r.FailureMessage, failureMessage)
                    .SetProperty(r => r.CompletedAt, completedAt)
                    .SetProperty(r => r.UpdatedAt, completedAt),
                cancellationToken);
        return failed == 1;
    }

    public Task AddItemsAsync(
        IEnumerable<VendorRecommendationItem> items,
        CancellationToken cancellationToken = default) =>
        _db.VendorRecommendationItems.AddRangeAsync(items, cancellationToken);

    public async Task<IReadOnlyList<Vendor>> ListApprovedByCategoriesAsync(
        IReadOnlyCollection<BusinessCategory> categories,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Vendors
            .AsNoTracking()
            .Where(v => v.Status == VendorStatus.APPROVED);

        if (categories.Count > 0)
            query = query.Where(v => categories.Contains(v.Category));

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VendorOffering>> ListOfferingsForVendorsAsync(
        IEnumerable<Guid> vendorIds,
        CancellationToken cancellationToken = default)
    {
        var ids = vendorIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        return await _db.VendorOfferings
            .AsNoTracking()
            .Where(o => ids.Contains(o.VendorId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VendorAvailability>> ListAvailabilityForVendorsAsync(
        IEnumerable<Guid> vendorIds,
        CancellationToken cancellationToken = default)
    {
        var ids = vendorIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        return await _db.VendorAvailabilities
            .AsNoTracking()
            .Where(a => ids.Contains(a.VendorId))
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
