using Application.Dtos.Plans;
using Domain.Entities;

namespace Application.Services.Planning;

public interface IPlanDecisionService
{
    Task<EventPlanDraft> ApprovePlanAsync(Guid planId, string? notes = null, CancellationToken cancellationToken = default);
    Task<EventPlanDraft> RejectPlanAsync(
        Guid planId,
        string remarks,
        RejectionSeverity severity,
        CancellationToken cancellationToken = default);
}
