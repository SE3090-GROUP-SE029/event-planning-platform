using Application.Common.Interfaces;
using Application.Dtos.Plans;
using Application.Services.Planning;
using Application.Services.Validation;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Planning;

public sealed class PlanGenerationService(
    IEventRepository eventRepository,
    IEventPlanDraftRepository planRepository,
    ICurrentUserService currentUser,
    IAgenticAiClient aiClient,
    ICoordinatorPlanValidationService validator,
    AppDbContext db,
    ILogger<PlanGenerationService> logger) : IPlanGenerationService
{
    public async Task<EventPlanDraft> GeneratePlanAsync(
        Guid eventId,
        CancellationToken cancellationToken = default,
        bool regenerate = false)
    {
        logger.LogInformation("Generating plan for event {EventId}", eventId);
        var eventEntity = await eventRepository.GetByIdAsync(eventId)
            ?? throw new KeyNotFoundException("Event not found.");
        EnsureAuthorized(eventEntity);

        var existingPlans = await planRepository.ListAsync(eventId, null, null, cancellationToken);
        if (existingPlans.Count > 0 && !regenerate)
            throw new InvalidOperationException("A plan already exists. Review it or explicitly regenerate it.");
        if (regenerate && existingPlans.FirstOrDefault()?.Status != PlanStatus.Rejected)
            throw new InvalidOperationException("Only a rejected plan can be regenerated.");

        var response = await aiClient.GeneratePlanAsync(eventEntity, cancellationToken);
        var validation = validator.ValidateCoordinatorPlan(response, eventEntity);
        if (!validation.IsValid)
            throw new InvalidOperationException(
                $"Generated plan failed validation: {string.Join("; ", validation.Errors)}");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var version = regenerate
            ? await CreateNextVersionAsync(eventId, existingPlans[0].Id, cancellationToken)
            : await planRepository.GetNextVersionAsync(eventId, cancellationToken);
        var now = DateTime.UtcNow;
        var plan = new EventPlanDraft
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Version = version,
            Status = PlanStatus.PendingPlannerReview,
            ServiceCategories = response.ServiceCategories,
            BudgetAllocation = response.BudgetAllocation,
            TargetVendorTypes = response.TargetVendorTypes,
            ProposedTimeline = response.ProposedTimeline,
            Rationale = response.Rationale ?? string.Empty,
            PlanCompletenessScore = response.PlanCompletenessScore,
            ValidationSummary = validation.Summary,
            EventSnapshot = EventSnapshot.FromEvent(eventEntity),
            GeneratedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedById = currentUser.UserId ?? throw new UnauthorizedAccessException("User identity is missing.")
        };

        foreach (var risk in response.IdentifiedRisks)
        {
            if (Enum.TryParse<RiskSeverity>(risk.Severity, true, out var severity))
                plan.IdentifiedRisks.Add(new IdentifiedRisk(risk.Risk ?? string.Empty, severity, risk.Recommendation ?? string.Empty));
        }
        foreach (var requirement in response.MissingRequirements)
        {
            plan.MissingRequirements.Add(new MissingRequirement(
                requirement.Requirement ?? string.Empty,
                requirement.Reason ?? string.Empty,
                RequirementCategory.Other));
        }

        await planRepository.AddAsync(plan, cancellationToken);
        await planRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Plan generation completed for event {EventId}, version {Version}", eventId, version);
        return plan;
    }

    public async Task<int> CreateNextVersionAsync(
        Guid eventId,
        Guid? planIdToSupersede = null,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await eventRepository.GetByIdAsync(eventId)
            ?? throw new KeyNotFoundException("Event not found.");
        EnsureAuthorized(eventEntity);

        if (planIdToSupersede.HasValue)
        {
            var oldPlan = await planRepository.GetByIdAsync(planIdToSupersede.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Plan to supersede was not found.");
            if (oldPlan.EventId != eventId)
                throw new InvalidOperationException("The plan to supersede belongs to a different event.");
            if (oldPlan.Status != PlanStatus.Rejected)
                throw new InvalidOperationException("Only a rejected plan can be superseded by regeneration.");
            oldPlan.Status = PlanStatus.Superseded;
            oldPlan.UpdatedAt = DateTime.UtcNow;
        }
        return await planRepository.GetNextVersionAsync(eventId, cancellationToken);
    }

    private void EnsureAuthorized(Event eventEntity)
    {
        if (currentUser.IsAdmin || currentUser.UserId != eventEntity.OwnerId)
            throw new UnauthorizedAccessException("You are not authorized to plan this event.");
    }
}
