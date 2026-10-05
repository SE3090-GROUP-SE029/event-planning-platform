using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IPlanGenerationJobRepository
{
    Task AddAsync(PlanGenerationJob job, CancellationToken cancellationToken = default);
    Task<PlanGenerationJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PlanGenerationJob?> GetLatestForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);
    Task<PlanGenerationJob?> GetActiveForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);
    Task<PlanGenerationJob?> GetNextRunnableAsync(
        DateTime staleProcessingBefore,
        CancellationToken cancellationToken = default);
    Task<bool> TryClaimAsync(
        Guid id,
        DateTime now,
        DateTime staleProcessingBefore,
        CancellationToken cancellationToken = default);
    Task<bool> MarkFailedIfProcessingAsync(
        Guid id,
        string failureMessage,
        DateTime completedAt,
        CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
