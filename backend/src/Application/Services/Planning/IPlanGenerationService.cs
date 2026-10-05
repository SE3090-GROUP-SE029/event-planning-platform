using Application.Dtos.Plans;
using Domain.Entities;

namespace Application.Services.Planning;

public interface IPlanGenerationService
{
    Task<EventPlanDraft> GeneratePlanAsync(
        Guid eventId,
        CancellationToken cancellationToken = default,
        bool regenerate = false);
    Task<EventPlanDraft> GeneratePlanAsync(
        Guid eventId,
        Guid requestedByUserId,
        Guid generationJobId,
        CancellationToken cancellationToken = default,
        bool regenerate = false);
    Task<int> CreateNextVersionAsync(Guid eventId, Guid? planIdToSupersede = null, CancellationToken cancellationToken = default);
}
