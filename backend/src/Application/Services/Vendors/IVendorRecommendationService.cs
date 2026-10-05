using Application.Dtos.Vendors;

namespace Application.Services.Vendors;

public interface IVendorRecommendationService
{
    Task<VendorRecommendationRunResponse> GenerateAsync(
        Guid eventId,
        Guid userId,
        GenerateVendorRecommendationsRequest? request,
        CancellationToken cancellationToken = default);

    Task<VendorRecommendationRunResponse?> GetLatestAsync(
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<VendorRecommendationRunResponse?> GetRunAsync(
        Guid eventId,
        Guid runId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default);
}
