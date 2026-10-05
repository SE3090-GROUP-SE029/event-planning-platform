namespace Application.Dtos.Vendors;

public class VendorRecommendationItemResponse
{
    public Guid VendorId { get; set; }
    public Guid? VendorServiceId { get; set; }
    public int Rank { get; set; }
    public int Score { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public decimal? Price { get; set; }
    public string? PricingType { get; set; }
    public decimal? AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public bool AvailabilityMatch { get; set; }
}

public class VendorRecommendationRunResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid EventPlanDraftId { get; set; }
    public int CandidateCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "Completed";
    public string Stage { get; set; } = string.Empty;
    public string? FailureMessage { get; set; }
    public string? SourceNote { get; set; }
    public bool FromCache { get; set; }
    public IReadOnlyList<VendorRecommendationItemResponse> Items { get; set; } = [];
}
