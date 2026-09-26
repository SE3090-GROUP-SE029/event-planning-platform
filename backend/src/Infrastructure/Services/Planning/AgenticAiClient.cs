using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Application.Dtos.Plans;
using Application.Services.Planning;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Planning;

public sealed class AgenticAiClient(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<AgenticAiClient> logger) : IAgenticAiClient
{
    public async Task<CoordinatorPlanResponse> GeneratePlanAsync(
        Event eventEntity,
        CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("AgenticAI");
        var request = new
        {
            eventId = eventEntity.Id,
            @event = new
            {
                name = eventEntity.PreferredVenue ?? "Event",
                type = eventEntity.EventType.ToString(),
                date = eventEntity.PreferredDate,
                location = eventEntity.PreferredVenue,
                guest_count = eventEntity.GuestCount,
                budget = eventEntity.Budget,
                requirements = eventEntity.Requirements
            }
        };

        using var response = await client.PostAsJsonAsync(
            configuration["AgenticAI:GeneratePath"] ?? "/api/coordinator/generate",
            request,
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Agentic AI returned HTTP {StatusCode} for event {EventId}",
                (int)response.StatusCode,
                eventEntity.Id);
            response.EnsureSuccessStatusCode();
        }

        var output = await response.Content.ReadFromJsonAsync<AgenticAiPlanResponse>(
            cancellationToken)
            ?? throw new InvalidOperationException("Agentic AI returned an empty response.");
        return output.ToCoordinatorPlanResponse();
    }

    private sealed record AgenticAiPlanResponse
    {
        [JsonPropertyName("service_categories")]
        public List<string> ServiceCategories { get; init; } = [];

        [JsonPropertyName("budget_allocation")]
        public List<AgenticAiBudgetAllocation> BudgetAllocation { get; init; } = [];

        [JsonPropertyName("target_vendor_types")]
        public List<string> TargetVendorTypes { get; init; } = [];

        [JsonPropertyName("proposed_timeline")]
        public List<AgenticAiTimelinePhase> ProposedTimeline { get; init; } = [];

        [JsonPropertyName("rationale")]
        public string? Rationale { get; init; }

        [JsonPropertyName("identified_risks")]
        public List<RiskDto> IdentifiedRisks { get; init; } = [];

        [JsonPropertyName("missing_requirements")]
        public List<MissingRequirementDto> MissingRequirements { get; init; } = [];

        [JsonPropertyName("plan_completeness_score")]
        public int PlanCompletenessScore { get; init; }

        [JsonPropertyName("validation_summary")]
        public string? ValidationSummary { get; init; }

        public CoordinatorPlanResponse ToCoordinatorPlanResponse()
        {
            try
            {
                return new CoordinatorPlanResponse
                {
                    ServiceCategories = ServiceCategories,
                    BudgetAllocation = BudgetAllocation.ToDictionary(
                        item => item.Category,
                        item => item.Amount,
                        StringComparer.OrdinalIgnoreCase),
                    TargetVendorTypes = TargetVendorTypes,
                    ProposedTimeline = ProposedTimeline.ToDictionary(
                        item => item.PhaseName,
                        item => $"{item.Timing}: {item.Description}"),
                    Rationale = Rationale,
                    IdentifiedRisks = IdentifiedRisks,
                    MissingRequirements = MissingRequirements,
                    PlanCompletenessScore = PlanCompletenessScore,
                    ValidationSummary = ValidationSummary
                };
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException(
                    "Agentic AI returned duplicate budget categories or timeline phases.", ex);
            }
        }
    }

    private sealed record AgenticAiBudgetAllocation
    {
        [JsonPropertyName("category")]
        public string Category { get; init; } = string.Empty;

        [JsonPropertyName("amount")]
        public decimal Amount { get; init; }
    }

    private sealed record AgenticAiTimelinePhase
    {
        [JsonPropertyName("phase_name")]
        public string PhaseName { get; init; } = string.Empty;

        [JsonPropertyName("timing")]
        public string Timing { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; init; } = string.Empty;
    }
}
