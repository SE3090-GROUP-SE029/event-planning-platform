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
    IPlanGenerationJobRepository generationJobRepository,
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
        var requestedByUserId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("User identity is missing.");
        return await GeneratePlanCoreAsync(
            eventId,
            requestedByUserId,
            null,
            cancellationToken,
            regenerate);
    }

    public async Task<EventPlanDraft> GeneratePlanAsync(
        Guid eventId,
        Guid requestedByUserId,
        Guid generationJobId,
        CancellationToken cancellationToken = default,
        bool regenerate = false) =>
        await GeneratePlanCoreAsync(
            eventId,
            requestedByUserId,
            generationJobId,
            cancellationToken,
            regenerate);

    private async Task<EventPlanDraft> GeneratePlanCoreAsync(
        Guid eventId,
        Guid requestedByUserId,
        Guid? generationJobId,
        CancellationToken cancellationToken,
        bool regenerate)
    {
        var generationStartedAt = DateTime.UtcNow;
        var eventEntity = await eventRepository.GetByIdAsync(eventId)
            ?? throw new KeyNotFoundException("Event not found.");
        EnsureAuthorized(eventEntity, requestedByUserId);
        var job = generationJobId.HasValue
            ? await generationJobRepository.GetByIdAsync(generationJobId.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Plan generation job was not found.")
            : null;
        if (job is not null &&
            (job.EventId != eventId ||
             job.RequestedById != requestedByUserId ||
             job.Status != PlanGenerationJobStatus.Processing))
            throw new InvalidOperationException("Plan generation job state does not match this request.");
        var requestId = job?.RequestId;
        logger.LogInformation(
            "Plan generation started for event {EventId}, generation job {GenerationJobId}, request {RequestId}",
            eventId,
            generationJobId,
            requestId);

        var existingPlans = await planRepository.ListAsync(eventId, null, null, cancellationToken);
        if (existingPlans.Count > 0 && !regenerate)
            throw new InvalidOperationException("A plan already exists. Review it or explicitly regenerate it.");
        if (regenerate && existingPlans.FirstOrDefault()?.Status != PlanStatus.Rejected)
            throw new InvalidOperationException("Only a rejected plan can be regenerated.");

        var response = await aiClient.GeneratePlanAsync(eventEntity, requestId, cancellationToken);
        logger.LogInformation(
            "Coordinator plan received for event {EventId}, generation job {GenerationJobId}, elapsed {ElapsedMilliseconds}ms",
            eventId,
            generationJobId,
            (long)(DateTime.UtcNow - generationStartedAt).TotalMilliseconds);
        var validation = validator.ValidateCoordinatorPlan(response, eventEntity);
        if (!validation.IsValid)
            throw new InvalidOperationException(
                $"Generated plan failed validation: {string.Join("; ", validation.Errors)}");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (regenerate)
        {
            var oldPlan = await planRepository.GetByIdAsync(existingPlans[0].Id, cancellationToken)
                ?? throw new KeyNotFoundException("Plan to supersede was not found.");
            if (oldPlan.EventId != eventId || oldPlan.Status != PlanStatus.Rejected)
                throw new InvalidOperationException("Only a rejected plan for this event can be regenerated.");
            oldPlan.Status = PlanStatus.Superseded;
            oldPlan.UpdatedAt = DateTime.UtcNow;
        }
        var version = await planRepository.GetNextVersionAsync(eventId, cancellationToken);
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
            CreatedById = requestedByUserId
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

        if (job is not null)
        {
            job.PlanId = plan.Id;
            job.Status = PlanGenerationJobStatus.Succeeded;
            job.FailureMessage = null;
            job.CompletedAt = now;
            job.UpdatedAt = now;
        }

        try
        {
            await planRepository.AddAsync(plan, cancellationToken);
            await planRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Plan save failed for event {EventId}, plan {PlanId}, generation job {GenerationJobId}, request {RequestId}, failure type {FailureType}",
                eventId,
                plan.Id,
                generationJobId,
                requestId,
                exception.GetType().Name);
            throw;
        }

        logger.LogInformation(
            "Plan save succeeded for event {EventId}, plan {PlanId}, generation job {GenerationJobId}, request {RequestId}, version {Version}",
            eventId,
            plan.Id,
            generationJobId,
            requestId,
            version);
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

    private void EnsureAuthorized(Event eventEntity, Guid? requestedByUserId = null)
    {
        var userId = requestedByUserId ?? currentUser.UserId;
        if (currentUser.IsAdmin || userId != eventEntity.OwnerId)
            throw new UnauthorizedAccessException("You are not authorized to plan this event.");
    }
}
