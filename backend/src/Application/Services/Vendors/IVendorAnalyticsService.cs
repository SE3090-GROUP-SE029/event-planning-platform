using Application.Dtos.Vendors;

namespace Application.Services.Vendors;

public interface IVendorAnalyticsService
{
    Task<VendorAnalyticsResponse> GetMyAnalyticsAsync(Guid vendorUserId);
}
