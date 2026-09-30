using Application.Dtos.Vendors;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Interfaces;

public interface IVendorRepository
{
    Task<Vendor?> GetByUserIdAsync(Guid userId);
    Task<Vendor?> GetByIdAsync(Guid id);
    Task<(IReadOnlyList<Vendor> Items, int TotalCount)> ListAdminAsync(
        AdminVendorQuery query,
        BusinessCategory? categoryFilter);
    Task AddAsync(Vendor vendor);
    Task SaveChangesAsync();
}
