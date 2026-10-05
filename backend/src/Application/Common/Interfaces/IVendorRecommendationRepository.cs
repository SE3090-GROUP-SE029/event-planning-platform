using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Interfaces;

public interface IVendorRecommendationRepository
{
    Task AddRunAsync(VendorRecommendationRun run, CancellationToken cancellationToken = default);
    Task<bool> TryAddRunAsync(
        VendorRecommendationRun run,
        CancellationToken cancellationToken = default);

    Task<VendorRecommendationRun?> GetLatestForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);
    Task<VendorRecommendationRun?> GetActiveForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);
    Task<VendorRecommendationRun?> GetByIdAsync(
        Guid runId,
        CancellationToken cancellationToken = default);
    Task<VendorRecommendationRun?> GetNextRunnableAsync(
        DateTime staleRunningBefore,
        CancellationToken cancellationToken = default);
    Task<bool> TryClaimAsync(
        Guid runId,
        DateTime now,
        DateTime staleRunningBefore,
        CancellationToken cancellationToken = default);
    Task<bool> UpdateProgressAsync(
        Guid runId,
        string stage,
        int? candidateCount,
        DateTime updatedAt,
        CancellationToken cancellationToken = default);
    Task<bool> MarkFailedIfRunningAsync(
        Guid runId,
        string failureMessage,
        DateTime completedAt,
        CancellationToken cancellationToken = default);
    Task AddItemsAsync(
        IEnumerable<VendorRecommendationItem> items,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Vendor>> ListApprovedByCategoriesAsync(
        IReadOnlyCollection<BusinessCategory> categories,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VendorOffering>> ListOfferingsForVendorsAsync(
        IEnumerable<Guid> vendorIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VendorAvailability>> ListAvailabilityForVendorsAsync(
        IEnumerable<Guid> vendorIds,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
