using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class PlanGenerationJobRepository(AppDbContext db) : IPlanGenerationJobRepository
{
    public async Task AddAsync(PlanGenerationJob job, CancellationToken cancellationToken = default) =>
        await db.PlanGenerationJobs.AddAsync(job, cancellationToken);

    public Task<PlanGenerationJob?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        db.PlanGenerationJobs.FirstOrDefaultAsync(job => job.Id == id, cancellationToken);

    public Task<PlanGenerationJob?> GetActiveForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default) =>
        db.PlanGenerationJobs
            .AsNoTracking()
            .Where(job =>
                job.EventId == eventId &&
                (job.Status == PlanGenerationJobStatus.Queued ||
                 job.Status == PlanGenerationJobStatus.Processing))
            .OrderByDescending(job => job.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<PlanGenerationJob?> GetLatestForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default) =>
        db.PlanGenerationJobs
            .AsNoTracking()
            .Where(job => job.EventId == eventId)
            .OrderByDescending(job => job.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<PlanGenerationJob?> GetNextRunnableAsync(
        DateTime staleProcessingBefore,
        CancellationToken cancellationToken = default) =>
        db.PlanGenerationJobs
            .AsNoTracking()
            .Where(job =>
                job.Status == PlanGenerationJobStatus.Queued ||
                (job.Status == PlanGenerationJobStatus.Processing &&
                 job.UpdatedAt <= staleProcessingBefore))
            .OrderBy(job => job.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> TryClaimAsync(
        Guid id,
        DateTime now,
        DateTime staleProcessingBefore,
        CancellationToken cancellationToken = default)
    {
        var claimed = await db.PlanGenerationJobs
            .Where(job =>
                job.Id == id &&
                (job.Status == PlanGenerationJobStatus.Queued ||
                 (job.Status == PlanGenerationJobStatus.Processing &&
                  job.UpdatedAt <= staleProcessingBefore)))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, PlanGenerationJobStatus.Processing)
                    .SetProperty(job => job.StartedAt, now)
                    .SetProperty(job => job.UpdatedAt, now)
                    .SetProperty(job => job.AttemptCount, job => job.AttemptCount + 1),
                cancellationToken);
        return claimed == 1;
    }

    public async Task<bool> MarkFailedIfProcessingAsync(
        Guid id,
        string failureMessage,
        DateTime completedAt,
        CancellationToken cancellationToken = default)
    {
        var updated = await db.PlanGenerationJobs
            .Where(job =>
                job.Id == id &&
                job.Status == PlanGenerationJobStatus.Processing)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, PlanGenerationJobStatus.Failed)
                    .SetProperty(job => job.FailureMessage, failureMessage)
                    .SetProperty(job => job.CompletedAt, completedAt)
                    .SetProperty(job => job.UpdatedAt, completedAt),
                cancellationToken);
        return updated == 1;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
