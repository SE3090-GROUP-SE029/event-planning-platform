namespace Domain.Entities;

/// <summary>A ranked vendor recommendation item within a recommendation run.</summary>
public class VendorRecommendationItem
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
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

    public VendorRecommendationRun Run { get; set; } = null!;
}
