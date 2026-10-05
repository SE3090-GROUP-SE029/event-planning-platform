using Domain.Entities;

namespace Application.Services.Planning;

public interface IPlanGenerationJobService
{
    Task<PlanGenerationJob> EnqueueAsync(
        Guid eventId,
        bool regenerate,
        string requestId,
        CancellationToken cancellationToken = default);

    Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default);
}
