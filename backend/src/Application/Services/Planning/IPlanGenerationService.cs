using Application.Dtos.Plans;
using Domain.Entities;

namespace Application.Services.Planning;

public interface IPlanGenerationService
{
    Task<EventPlanDraft> GeneratePlanAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<int> CreateNextVersionAsync(Guid eventId, Guid? planIdToSupersede = null, CancellationToken cancellationToken = default);
}
