using Application.Dtos.Plans;
using Domain.Entities;

namespace Application.Services.Planning;

public interface IAgenticAiClient
{
    Task<CoordinatorPlanResponse> GeneratePlanAsync(
        Event eventEntity,
        CancellationToken cancellationToken = default);

    Task<CoordinatorPlanResponse> GeneratePlanAsync(
        Event eventEntity,
        string? requestId,
        CancellationToken cancellationToken = default) =>
        GeneratePlanAsync(eventEntity, cancellationToken);
}
