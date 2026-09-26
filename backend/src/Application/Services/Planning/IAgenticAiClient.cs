using Application.Dtos.Plans;
using Domain.Entities;

namespace Application.Services.Planning;

public interface IAgenticAiClient
{
    Task<CoordinatorPlanResponse> GeneratePlanAsync(
        Event eventEntity,
        CancellationToken cancellationToken = default);
}
