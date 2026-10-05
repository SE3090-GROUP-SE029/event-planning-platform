using System.Net;
using Application.Common.Interfaces;
using Application.Dtos.Vendors;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Services.Vendors;

public sealed class VendorRecommendationService : IVendorRecommendationService
{
    private readonly IEventRepository _events;
    private readonly IEventPlanDraftRepository _plans;
    private readonly IVendorRecommendationRepository _recommendations;
    private readonly IVendorRatingRepository _ratings;
    private readonly IVendorAnalysisAiClient _ai;
    private readonly ILogger<VendorRecommendationService> _logger;

    public VendorRecommendationService(
        IEventRepository events,
        IEventPlanDraftRepository plans,
        IVendorRecommendationRepository recommendations,
        IVendorRatingRepository ratings,
        IVendorAnalysisAiClient ai,
        ILogger<VendorRecommendationService> logger)
    {
        _events = events;
        _plans = plans;
        _recommendations = recommendations;
        _ratings = ratings;
        _ai = ai;
        _logger = logger;
    }

    public async Task<VendorRecommendationRunResponse> GenerateAsync(
        Guid eventId,
        Guid userId,
        GenerateVendorRecommendationsRequest? request,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await RequireOwnedEventAsync(eventId, userId);
        var plan = await RequireApprovedPlanAsync(eventId, request?.PlanId, cancellationToken);

        var active = await _recommendations.GetActiveForEventAsync(eventId, cancellationToken);
        if (active is not null)
            return MapRun(active, fromCache: false);

        var latest = await _recommendations.GetLatestForEventAsync(eventId, cancellationToken);
        if (request?.ForceRefresh != true &&
            latest is not null &&
            latest.EventPlanDraftId == plan.Id &&
            latest.Status == VendorRecommendationRun.CompletedStatus)
        {
            return MapRun(latest, fromCache: false);
        }

        var now = DateTime.UtcNow;
        var run = new VendorRecommendationRun
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            EventPlanDraftId = plan.Id,
            RequestedByUserId = userId,
            CreatedAt = now,
            UpdatedAt = now,
            Status = VendorRecommendationRun.PendingStatus,
            Stage = "Preparing recommendations"
        };

        if (await _recommendations.TryAddRunAsync(run, cancellationToken))
        {
            _logger.LogInformation(
                "Vendor recommendation GenerationQueued event {EventId}, run {RunId}, plan {PlanId}",
                eventId,
                run.Id,
                plan.Id);
            return MapRun(run, fromCache: false);
        }

        active = await _recommendations.GetActiveForEventAsync(eventId, cancellationToken);
        if (active is not null)
            return MapRun(active, fromCache: false);

        latest = await _recommendations.GetLatestForEventAsync(eventId, cancellationToken);
        if (latest is not null &&
            latest.EventPlanDraftId == plan.Id &&
            latest.Status == VendorRecommendationRun.CompletedStatus)
            return MapRun(latest, fromCache: false);

        throw new InvalidOperationException(
            "A concurrent recommendation request could not be resolved. Please retry.");
    }

    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var staleRunningBefore = now.AddMinutes(-35);
        var candidate = await _recommendations.GetNextRunnableAsync(
            staleRunningBefore,
            cancellationToken);
        if (candidate is null)
            return false;

        if (!await _recommendations.TryClaimAsync(
                candidate.Id,
                now,
                staleRunningBefore,
                cancellationToken))
            return true;

        var run = await _recommendations.GetByIdAsync(candidate.Id, cancellationToken)
            ?? throw new InvalidOperationException("Claimed recommendation run disappeared.");
        _logger.LogInformation(
            "Vendor recommendation GenerationStarted event {EventId}, run {RunId}, plan {PlanId}",
            run.EventId,
            run.Id,
            run.EventPlanDraftId);

        try
        {
            await ProcessRunAsync(run, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Vendor recommendation processing cancelled event {EventId}, run {RunId}; it will be resumed after its lease expires",
                run.EventId,
                run.Id);
            throw;
        }
        catch (Exception exception)
        {
            var failureMessage = GetFailureMessage(exception);
            await _recommendations.MarkFailedIfRunningAsync(
                run.Id,
                failureMessage,
                DateTime.UtcNow,
                cancellationToken);
            _logger.LogError(
                exception,
                "Vendor recommendation GenerationFailed event {EventId}, run {RunId}, failure type {FailureType}",
                run.EventId,
                run.Id,
                exception.GetType().Name);
            if (exception is OperationCanceledException or
                HttpRequestException { StatusCode: HttpStatusCode.GatewayTimeout })
            {
                _logger.LogWarning(
                    "Vendor recommendation Timeout event {EventId}, run {RunId}, failure type {FailureType}",
                    run.EventId,
                    run.Id,
                    exception.GetType().Name);
            }
            return true;
        }
    }

    public async Task<VendorRecommendationRunResponse?> GetLatestAsync(
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await RequireOwnedEventAsync(eventId, userId);
        var run = await _recommendations.GetLatestForEventAsync(eventId, cancellationToken);
        return run is null ? null : MapRun(run, fromCache: false);
    }

    public async Task<VendorRecommendationRunResponse?> GetRunAsync(
        Guid eventId,
        Guid runId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await RequireOwnedEventAsync(eventId, userId);
        var run = await _recommendations.GetByIdAsync(runId, cancellationToken);
        return run?.EventId == eventId ? MapRun(run, fromCache: false) : null;
    }

    private async Task ProcessRunAsync(
        VendorRecommendationRun run,
        CancellationToken cancellationToken)
    {
        var eventEntity = await RequireOwnedEventAsync(
            run.EventId,
            run.RequestedByUserId);
        var plan = await RequireApprovedPlanAsync(
            run.EventId,
            run.EventPlanDraftId,
            cancellationToken);
        var categories = VendorRecommendationCandidateBuilder.ResolveCategories(
            plan.ServiceCategories,
            plan.TargetVendorTypes);

        await UpdateProgressAsync(run.Id, "Finding candidate vendors", null, cancellationToken);
        var candidates = await BuildCandidatesAsync(plan, categories, cancellationToken);
        if (candidates.Count == 0)
            throw new InvalidOperationException(
                "No approved vendors match this plan's categories, budget, and availability constraints.");

        await UpdateProgressAsync(
            run.Id,
            "Analyzing services",
            candidates.Count,
            cancellationToken);
        var aiRequest = ToAiRequest(eventEntity.Id, plan, candidates);
        await UpdateProgressAsync(run.Id, "Ranking vendors", null, cancellationToken);
        await UpdateProgressAsync(run.Id, "Generating recommendations", null, cancellationToken);

        var aiResponse = await _ai.RecommendAsync(aiRequest, cancellationToken);
        var validated = ValidateAiResponse(aiResponse, candidates);
        if (validated.Count == 0)
            throw new InvalidOperationException(
                "The AI service returned no recommendations matching the candidate vendors.");

        await UpdateProgressAsync(run.Id, "Almost complete", null, cancellationToken);
        var completed = PersistRun(
            run.EventId,
            run.EventPlanDraftId,
            run.RequestedByUserId,
            candidates.Count,
            validated);
        run.CandidateCount = completed.CandidateCount;
        run.Items = completed.Items;
        foreach (var item in run.Items)
        {
            item.RunId = run.Id;
            item.Run = run;
        }
        await _recommendations.AddItemsAsync(run.Items, cancellationToken);
        run.Status = VendorRecommendationRun.CompletedStatus;
        run.Stage = VendorRecommendationRun.CompletedStatus;
        run.FailureMessage = null;
        run.CompletedAt = DateTime.UtcNow;
        run.UpdatedAt = run.CompletedAt.Value;
        try
        {
            await _recommendations.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Vendor recommendation SaveFailure event {EventId}, run {RunId}, plan {PlanId}",
                run.EventId,
                run.Id,
                run.EventPlanDraftId);
            throw;
        }
        _logger.LogInformation(
            "Vendor recommendation SaveSuccess event {EventId}, run {RunId}, plan {PlanId}, candidate count {CandidateCount}, result count {ResultCount}",
            run.EventId,
            run.Id,
            run.EventPlanDraftId,
            run.CandidateCount,
            run.Items.Count);
        _logger.LogInformation(
            "Vendor recommendation GenerationCompleted event {EventId}, run {RunId}, plan {PlanId}",
            run.EventId,
            run.Id,
            run.EventPlanDraftId);
    }

    private async Task UpdateProgressAsync(
        Guid runId,
        string stage,
        int? candidateCount,
        CancellationToken cancellationToken)
    {
        if (!await _recommendations.UpdateProgressAsync(
                runId,
                stage,
                candidateCount,
                DateTime.UtcNow,
                cancellationToken))
            throw new InvalidOperationException("Recommendation run is no longer running.");
    }

    private static string GetFailureMessage(Exception exception) =>
        exception switch
        {
            OperationCanceledException =>
                "Vendor recommendation timed out. Please retry.",
            HttpRequestException { StatusCode: HttpStatusCode.GatewayTimeout } =>
                "Vendor recommendation timed out. Please retry.",
            _ when !string.IsNullOrWhiteSpace(exception.Message) =>
                exception.Message.Length <= 1000
                    ? exception.Message
                    : exception.Message[..1000],
            _ => "Vendor recommendation failed. Please retry."
        };

    private async Task<Event> RequireOwnedEventAsync(Guid eventId, Guid userId)
    {
        var eventEntity = await _events.GetByIdAsync(eventId)
            ?? throw new KeyNotFoundException("Event not found.");

        if (eventEntity.OwnerId != userId)
            throw new UnauthorizedAccessException("You do not own this event.");

        return eventEntity;
    }

    private async Task<EventPlanDraft> RequireApprovedPlanAsync(
        Guid eventId,
        Guid? planId,
        CancellationToken cancellationToken)
    {
        if (planId.HasValue && planId.Value != Guid.Empty)
        {
            var plan = await _plans.GetByIdAsync(planId.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Plan not found.");

            if (plan.EventId != eventId)
                throw new InvalidOperationException("Plan does not belong to this event.");

            if (plan.Status != PlanStatus.Approved)
                throw new InvalidOperationException("Vendor recommendations require an Approved event plan.");

            return plan;
        }

        var approved = await _plans.ListAsync(eventId, PlanStatus.Approved, version: null, cancellationToken);
        var latest = approved.OrderByDescending(p => p.Version).FirstOrDefault()
            ?? throw new InvalidOperationException(
                "No Approved event plan found. Approve a plan before requesting vendor recommendations.");

        return await _plans.GetByIdAsync(latest.Id, cancellationToken) ?? latest;
    }

    private async Task<IReadOnlyList<VendorRecommendationCandidateBuilder.Candidate>> BuildCandidatesAsync(
        EventPlanDraft plan,
        IReadOnlyList<BusinessCategory> categories,
        CancellationToken cancellationToken)
    {
        var approved = await _recommendations.ListApprovedByCategoriesAsync(categories, cancellationToken);
        if (approved.Count == 0)
            return [];

        var vendorIds = approved.Select(v => v.Id).ToList();
        var offerings = await _recommendations.ListOfferingsForVendorsAsync(vendorIds, cancellationToken);
        var availability = await _recommendations.ListAvailabilityForVendorsAsync(vendorIds, cancellationToken);
        var ratings = await _ratings.GetAggregatesForVendorsAsync(vendorIds);

        var offeringsByVendor = offerings
            .GroupBy(o => o.VendorId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<VendorOffering>)g.ToList());

        var availabilityByVendor = availability
            .GroupBy(a => a.VendorId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<VendorAvailability>)g.ToList());

        var snapshot = plan.EventSnapshot;
        return VendorRecommendationCandidateBuilder.Build(
            approved,
            offeringsByVendor,
            availabilityByVendor,
            ratings,
            categories,
            plan.BudgetAllocation,
            snapshot.Budget,
            snapshot.GuestCount,
            snapshot.EventDate);
    }

    private static VendorAnalysisRecommendRequest ToAiRequest(
        Guid eventId,
        EventPlanDraft plan,
        IReadOnlyList<VendorRecommendationCandidateBuilder.Candidate> candidates)
    {
        var snapshot = plan.EventSnapshot;
        return new VendorAnalysisRecommendRequest
        {
            EventId = eventId,
            PlanId = plan.Id,
            Plan = new VendorAnalysisPlanPayload
            {
                EventType = snapshot.EventType.ToString(),
                EventDate = snapshot.EventDate.ToString("O"),
                GuestCount = snapshot.GuestCount,
                Budget = snapshot.Budget,
                Requirements = snapshot.Requirements.Count == 0
                    ? null
                    : string.Join("; ", snapshot.Requirements),
                ServiceCategories = plan.ServiceCategories,
                TargetVendorTypes = plan.TargetVendorTypes,
                BudgetAllocation = plan.BudgetAllocation
            },
            Candidates = candidates.Select(c => new VendorAnalysisCandidatePayload
            {
                VendorId = c.Vendor.Id,
                BusinessName = c.Vendor.BusinessName,
                Category = c.Vendor.Category.ToString(),
                Description = Truncate(c.Vendor.Description, 500),
                AverageRating = c.AverageRating,
                ReviewCount = c.ReviewCount,
                AvailabilityMatch = c.AvailabilityMatch,
                BudgetFit = c.BudgetFit,
                MatchedService = c.Offering is null
                    ? null
                    : new VendorAnalysisServicePayload
                    {
                        VendorServiceId = c.Offering.Id,
                        ServiceName = c.Offering.ServiceName,
                        Price = c.Offering.Price,
                        PricingType = c.Offering.PricingType?.ToString(),
                        EffectivePrice = c.EffectivePrice
                    }
            }).ToList()
        };
    }

    private static List<(VendorRecommendationCandidateBuilder.Candidate Candidate, VendorAnalysisRecommendItemDto Ai)>
        ValidateAiResponse(
            VendorAnalysisRecommendResponse aiResponse,
            IReadOnlyList<VendorRecommendationCandidateBuilder.Candidate> candidates)
    {
        var byId = candidates.ToDictionary(c => c.Vendor.Id);
        var results = new List<(VendorRecommendationCandidateBuilder.Candidate, VendorAnalysisRecommendItemDto)>();
        var seen = new HashSet<Guid>();

        foreach (var item in aiResponse.Recommendations
                     .OrderByDescending(r => r.Score)
                     .ThenBy(r => r.VendorId))
        {
            if (!byId.TryGetValue(item.VendorId, out var candidate))
                continue;

            if (!seen.Add(item.VendorId))
                continue;

            var score = Math.Clamp(item.Score, 0, 100);
            Guid? serviceId = item.VendorServiceId;
            if (serviceId.HasValue)
            {
                if (candidate.Offering is null || candidate.Offering.Id != serviceId.Value)
                    serviceId = candidate.Offering?.Id;
            }
            else
            {
                serviceId = candidate.Offering?.Id;
            }

            var reasons = item.Reasons
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Take(3)
                .ToList();

            if (reasons.Count == 0)
                reasons.Add("Recommended based on your approved event plan.");

            results.Add((candidate, new VendorAnalysisRecommendItemDto
            {
                VendorId = candidate.Vendor.Id,
                VendorServiceId = serviceId,
                Score = score,
                Reasons = reasons
            }));
        }

        return results;
    }

    private static VendorRecommendationRun PersistRun(
        Guid eventId,
        Guid planId,
        Guid userId,
        int candidateCount,
        List<(VendorRecommendationCandidateBuilder.Candidate Candidate, VendorAnalysisRecommendItemDto Ai)> ranked)
    {
        var runId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var items = new List<VendorRecommendationItem>();
        var rank = 1;

        foreach (var (candidate, ai) in ranked)
        {
            var reason = string.Join(" ", ai.Reasons);
            if (reason.Length > 1000)
                reason = reason[..1000];

            items.Add(new VendorRecommendationItem
            {
                Id = Guid.NewGuid(),
                RunId = runId,
                VendorId = candidate.Vendor.Id,
                VendorServiceId = ai.VendorServiceId,
                Rank = rank++,
                Score = ai.Score,
                Reason = reason,
                BusinessName = candidate.Vendor.BusinessName,
                Category = candidate.Vendor.Category.ToString(),
                ServiceName = candidate.Offering?.ServiceName,
                Price = candidate.Offering?.Price,
                PricingType = candidate.Offering?.PricingType?.ToString(),
                AverageRating = candidate.AverageRating,
                ReviewCount = candidate.ReviewCount,
                AvailabilityMatch = candidate.AvailabilityMatch
            });
        }

        return new VendorRecommendationRun
        {
            Id = runId,
            EventId = eventId,
            EventPlanDraftId = planId,
            RequestedByUserId = userId,
            CandidateCount = candidateCount,
            CreatedAt = now,
            Items = items
        };
    }

    private static VendorRecommendationRunResponse MapRun(VendorRecommendationRun run, bool fromCache)
    {
        return new VendorRecommendationRunResponse
        {
            Id = run.Id,
            EventId = run.EventId,
            EventPlanDraftId = run.EventPlanDraftId,
            CandidateCount = run.CandidateCount,
            CreatedAt = run.CreatedAt,
            Status = run.Status,
            Stage = run.Stage,
            FailureMessage = run.FailureMessage,
            SourceNote = run.SourceNote,
            FromCache = fromCache,
            Items = run.Items
                .OrderBy(i => i.Rank)
                .Select(i => new VendorRecommendationItemResponse
                {
                    VendorId = i.VendorId,
                    VendorServiceId = i.VendorServiceId,
                    Rank = i.Rank,
                    Score = i.Score,
                    Reason = i.Reason,
                    BusinessName = i.BusinessName,
                    Category = i.Category,
                    ServiceName = i.ServiceName,
                    Price = i.Price,
                    PricingType = i.PricingType,
                    AverageRating = i.AverageRating,
                    ReviewCount = i.ReviewCount,
                    AvailabilityMatch = i.AvailabilityMatch
                })
                .ToList()
        };
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
