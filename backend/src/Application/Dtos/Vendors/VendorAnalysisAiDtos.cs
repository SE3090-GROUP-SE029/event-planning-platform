using System.Text.Json.Serialization;

namespace Application.Dtos.Vendors;

public sealed class VendorAnalysisPlanPayload
{
    [JsonPropertyName("eventType")]
    public string? EventType { get; init; }

    [JsonPropertyName("eventDate")]
    public string? EventDate { get; init; }

    [JsonPropertyName("guestCount")]
    public int? GuestCount { get; init; }

    [JsonPropertyName("budget")]
    public decimal? Budget { get; init; }

    [JsonPropertyName("requirements")]
    public string? Requirements { get; init; }

    [JsonPropertyName("serviceCategories")]
    public List<string> ServiceCategories { get; init; } = [];

    [JsonPropertyName("targetVendorTypes")]
    public List<string> TargetVendorTypes { get; init; } = [];

    [JsonPropertyName("budgetAllocation")]
    public Dictionary<string, decimal> BudgetAllocation { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class VendorAnalysisServicePayload
{
    [JsonPropertyName("vendorServiceId")]
    public Guid VendorServiceId { get; init; }

    [JsonPropertyName("serviceName")]
    public string ServiceName { get; init; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal? Price { get; init; }

    [JsonPropertyName("pricingType")]
    public string? PricingType { get; init; }

    [JsonPropertyName("effectivePrice")]
    public decimal? EffectivePrice { get; init; }
}

public sealed class VendorAnalysisCandidatePayload
{
    [JsonPropertyName("vendorId")]
    public Guid VendorId { get; init; }

    [JsonPropertyName("businessName")]
    public string BusinessName { get; init; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("averageRating")]
    public decimal? AverageRating { get; init; }

    [JsonPropertyName("reviewCount")]
    public int ReviewCount { get; init; }

    [JsonPropertyName("availabilityMatch")]
    public bool AvailabilityMatch { get; init; }

    [JsonPropertyName("matchedService")]
    public VendorAnalysisServicePayload? MatchedService { get; init; }

    [JsonPropertyName("budgetFit")]
    public bool BudgetFit { get; init; } = true;
}

public sealed class VendorAnalysisRecommendRequest
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; init; }

    [JsonPropertyName("planId")]
    public Guid PlanId { get; init; }

    [JsonPropertyName("plan")]
    public VendorAnalysisPlanPayload Plan { get; init; } = new();

    [JsonPropertyName("candidates")]
    public List<VendorAnalysisCandidatePayload> Candidates { get; init; } = [];
}

public sealed class VendorAnalysisRecommendItemDto
{
    [JsonPropertyName("vendorId")]
    public Guid VendorId { get; init; }

    [JsonPropertyName("vendorServiceId")]
    public Guid? VendorServiceId { get; init; }

    [JsonPropertyName("score")]
    public int Score { get; init; }

    [JsonPropertyName("reasons")]
    public List<string> Reasons { get; init; } = [];
}

public sealed class VendorAnalysisRecommendResponse
{
    [JsonPropertyName("recommendations")]
    public List<VendorAnalysisRecommendItemDto> Recommendations { get; init; } = [];
}
