using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IVendorAvailabilityRepository
{
    Task<IReadOnlyList<VendorAvailability>> ListByVendorIdAsync(Guid vendorId);
    Task<VendorAvailability?> GetByIdAsync(Guid id);
    Task<bool> HasOverlapAsync(Guid vendorId, DateTime start, DateTime end, Guid? excludeId = null);
    Task AddAsync(VendorAvailability availability);
    void Remove(VendorAvailability availability);
    Task SaveChangesAsync();
}
