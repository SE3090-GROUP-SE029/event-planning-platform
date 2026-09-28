using Application.Dtos.Vendors;

namespace Application.Services.Vendors;

public interface IVendorAnalysisAiClient
{
    Task<VendorAnalysisRecommendResponse> RecommendAsync(
        VendorAnalysisRecommendRequest request,
        CancellationToken cancellationToken = default);
}
