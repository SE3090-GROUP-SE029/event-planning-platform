namespace Application.Dtos.Vendors;

public class GenerateVendorRecommendationsRequest
{
    public Guid? PlanId { get; set; }
    public bool ForceRefresh { get; set; }
}
