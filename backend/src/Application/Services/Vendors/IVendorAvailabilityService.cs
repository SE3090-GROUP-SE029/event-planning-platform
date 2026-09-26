using Application.Dtos.Vendors;

namespace Application.Services.Vendors;

public interface IVendorAvailabilityService
{
    Task<VendorAvailabilityResponse> CreateAsync(Guid userId, CreateVendorAvailabilityRequest request);
    Task<IReadOnlyList<VendorAvailabilityResponse>> ListMineAsync(Guid userId);
    Task<VendorAvailabilityResponse> UpdateAsync(Guid userId, Guid availabilityId, UpdateVendorAvailabilityRequest request);
    Task DeleteAsync(Guid userId, Guid availabilityId);
}
