using Application.Common.Interfaces;
using Domain.Enums;

namespace Application.Services.Admin;

public sealed class AdminVendorApprovalService(IVendorRepository vendors) : IAdminVendorApprovalService
{
    public async Task ApproveAsync(Guid vendorId)
    {
        var vendor = await vendors.GetByIdAsync(vendorId)
            ?? throw new KeyNotFoundException("Vendor not found.");

        if (vendor.Status != VendorStatus.PENDING)
        {
            throw new InvalidOperationException("Only pending vendors can be approved.");
        }

        vendor.Status = VendorStatus.APPROVED;
        vendor.UpdatedAt = DateTime.UtcNow;
        await vendors.SaveChangesAsync();
    }
}
