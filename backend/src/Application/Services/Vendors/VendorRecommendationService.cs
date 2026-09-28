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

        var categories = VendorRecommendationCandidateBuilder.ResolveCategories(
            plan.ServiceCategories,
            plan.TargetVendorTypes);

        var candidates = await BuildCandidatesAsync(plan, categories, cancellationToken);

        if (candidates.Count == 0)
            throw new InvalidOperationException(
                "No APPROVED vendors match this plan's categories, budget, and availability constraints.");

        var aiRequest = ToAiRequest(eventEntity.Id, plan, candidates);

        try
        {
            var aiResponse = await _ai.RecommendAsync(aiRequest, cancellationToken);
            var validated = ValidateAiResponse(aiResponse, candidates);
            if (validated.Count == 0)
                throw new InvalidOperationException(
                    "The AI service returned no recommendations matching the candidate vendors.");

            var run = PersistRun(eventId, plan.Id, userId, candidates.Count, validated);
            await _recommendations.AddRunAsync(run, cancellationToken);
            await _recommendations.SaveChangesAsync(cancellationToken);
            return MapRun(run, fromCache: false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Vendor recommendation AI failed for event {EventId}", eventId);
            var previous = await _recommendations.GetLatestForEventAsync(eventId, cancellationToken);
            if (previous is not null)
            {
                var cached = MapRun(previous, fromCache: true);
                cached.SourceNote =
                    "Showing the last saved recommendations because the AI service is temporarily unavailable.";
                return cached;
            }

            if (ex is TaskCanceledException)
            {
                throw new HttpRequestException(
                    "The AI recommendation service timed out.",
                    ex,
                    HttpStatusCode.GatewayTimeout);
            }

            throw;
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
        // SourceNote is set by caller for cache fallback; entity SourceNote used otherwise.
        return new VendorRecommendationRunResponse
        {
            Id = run.Id,
            EventId = run.EventId,
            EventPlanDraftId = run.EventPlanDraftId,
            CandidateCount = run.CandidateCount,
            CreatedAt = run.CreatedAt,
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
