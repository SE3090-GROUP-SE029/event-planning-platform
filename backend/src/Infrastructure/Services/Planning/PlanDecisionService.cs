using Application.Common.Interfaces;
using Application.Dtos.Plans;
using Application.Services.Planning;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Planning;

public sealed class PlanDecisionService(
    IEventPlanDraftRepository planRepository,
    ICurrentUserService currentUser,
    ILogger<PlanDecisionService> logger) : IPlanDecisionService
{
    public async Task<EventPlanDraft> ApprovePlanAsync(
        Guid planId,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var plan = await GetAuthorizedPlan(planId, cancellationToken);
        if (plan.Status == PlanStatus.Approved) return plan;
        if (!PlanStatusRules.CanBeApproved(plan.Status))
            throw new InvalidOperationException($"Cannot approve plan in {plan.Status} status.");

        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("User identity is missing.");
        plan.Status = PlanStatus.Approved;
        plan.ApprovedById = userId;
        plan.PlannerRemarks = notes;
        plan.PlannerDecisionAt = DateTime.UtcNow;
        plan.UpdatedAt = DateTime.UtcNow;
        await planRepository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Plan {PlanId} approved by {UserId}", planId, userId);
        return plan;
    }

    public async Task<EventPlanDraft> RejectPlanAsync(
        Guid planId,
        string remarks,
        RejectionSeverity severity,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(remarks) || remarks.Trim().Length < 20)
            throw new ArgumentException("Rejection remarks must contain at least 20 characters.", nameof(remarks));
        var plan = await GetAuthorizedPlan(planId, cancellationToken);
        if (plan.Status == PlanStatus.Rejected) return plan;
        if (!PlanStatusRules.CanBeRejected(plan.Status))
            throw new InvalidOperationException($"Cannot reject plan in {plan.Status} status.");

        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("User identity is missing.");
        plan.Status = PlanStatus.Rejected;
        plan.RejectedById = userId;
        plan.PlannerRemarks = $"{severity}: {remarks.Trim()}";
        plan.PlannerDecisionAt = DateTime.UtcNow;
        plan.UpdatedAt = DateTime.UtcNow;
        await planRepository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Plan {PlanId} rejected by {UserId}", planId, userId);
        return plan;
    }

    private async Task<EventPlanDraft> GetAuthorizedPlan(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await planRepository.GetByIdAsync(planId, cancellationToken)
            ?? throw new KeyNotFoundException("Plan not found.");
        if (!currentUser.IsAdmin && currentUser.UserId != plan.Event.OwnerId)
            throw new UnauthorizedAccessException("You are not authorized to decide on this plan.");
        return plan;
    }
}
