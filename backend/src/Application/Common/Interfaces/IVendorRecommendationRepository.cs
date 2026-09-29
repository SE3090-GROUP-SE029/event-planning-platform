using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Interfaces;

public interface IVendorRecommendationRepository
{
    Task AddRunAsync(VendorRecommendationRun run, CancellationToken cancellationToken = default);

    Task<VendorRecommendationRun?> GetLatestForEventAsync(
        Guid eventId,
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
