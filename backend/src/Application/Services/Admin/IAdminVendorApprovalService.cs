namespace Application.Services.Admin;

public interface IAdminVendorApprovalService
{
    Task ApproveAsync(Guid vendorId);
}
