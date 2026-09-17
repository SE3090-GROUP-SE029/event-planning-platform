using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IVendorRepository
{
    Task<Vendor?> GetByUserIdAsync(Guid userId);
    Task<Vendor?> GetByIdAsync(Guid id);
    Task AddAsync(Vendor vendor);
    Task SaveChangesAsync();
}
