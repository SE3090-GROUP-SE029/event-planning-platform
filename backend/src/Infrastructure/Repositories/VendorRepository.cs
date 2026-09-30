using Application.Common.Interfaces;
using Application.Dtos.Vendors;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class VendorRepository : IVendorRepository
{
    private readonly AppDbContext _db;

    public VendorRepository(AppDbContext db) => _db = db;

    public Task<Vendor?> GetByUserIdAsync(Guid userId) =>
        _db.Vendors.FirstOrDefaultAsync(v => v.UserId == userId);

    public Task<Vendor?> GetByIdAsync(Guid id) =>
        _db.Vendors.FirstOrDefaultAsync(v => v.Id == id);

    public async Task<(IReadOnlyList<Vendor> Items, int TotalCount)> ListAdminAsync(
        AdminVendorQuery query,
        BusinessCategory? categoryFilter)
    {
        IQueryable<Vendor> vendors = _db.Vendors.AsNoTracking();

        if (query.Status.HasValue)
        {
            vendors = vendors.Where(v => v.Status == query.Status.Value);
        }

        if (categoryFilter.HasValue)
        {
            vendors = vendors.Where(v => v.Category == categoryFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            if (Guid.TryParse(search, out var searchedId))
            {
                vendors = vendors.Where(v => v.Id == searchedId || v.UserId == searchedId);
            }
            else
            {
                vendors = vendors.Where(v =>
                    v.BusinessName.ToLower().Contains(search) ||
                    v.ContactEmail.ToLower().Contains(search));
            }
        }

        var descending = string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        vendors = query.SortBy?.ToLowerInvariant() switch
        {
            "businessname" => descending
                ? vendors.OrderByDescending(v => v.BusinessName)
                : vendors.OrderBy(v => v.BusinessName),
            "status" => descending
                ? vendors.OrderByDescending(v => v.Status)
                : vendors.OrderBy(v => v.Status),
            _ => descending
                ? vendors.OrderByDescending(v => v.CreatedAt)
                : vendors.OrderBy(v => v.CreatedAt)
        };

        var totalCount = await vendors.CountAsync();
        var items = await vendors
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(Vendor vendor) => await _db.Vendors.AddAsync(vendor);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
