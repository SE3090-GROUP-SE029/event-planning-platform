using Application.Dtos.Vendors;

namespace Application.Services.Vendors;

public interface IAdminVendorService
{
    Task<AdminVendorListResponse> ListAsync(AdminVendorQuery query);
    Task<AdminVendorResponse> GetByIdAsync(Guid vendorId);
    Task<AdminVendorResponse> ApproveAsync(Guid vendorId);
    Task<AdminVendorResponse> SuspendAsync(Guid vendorId);
    Task<AdminVendorResponse> RestoreAsync(Guid vendorId);
}
