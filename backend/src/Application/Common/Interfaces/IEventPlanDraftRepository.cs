using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Interfaces;

public interface IEventPlanDraftRepository
{
    Task<EventPlanDraft?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EventPlanDraft>> ListAsync(
        Guid? eventId,
        PlanStatus? status,
        int? version,
        CancellationToken cancellationToken = default);
    Task<int> GetNextVersionAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task AddAsync(EventPlanDraft plan, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
