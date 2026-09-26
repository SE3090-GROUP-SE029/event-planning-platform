using Application.Common.Interfaces;
using Application.Dtos.Vendors;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class VendorMarketplaceRepository : IVendorMarketplaceRepository
{
    private readonly AppDbContext _db;

    public VendorMarketplaceRepository(AppDbContext db) => _db = db;

    public async Task<(IReadOnlyList<MarketplaceVendorListItemResponse> Items, int TotalCount)> SearchApprovedAsync(
        VendorMarketplaceQuery query,
        BusinessCategory? categoryFilter)
    {
        var vendors = _db.Vendors
            .AsNoTracking()
            .Where(v => v.Status == VendorStatus.APPROVED);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            vendors = vendors.Where(v => v.BusinessName.ToLower().Contains(search));
        }

        if (categoryFilter.HasValue)
        {
            vendors = vendors.Where(v => v.Category == categoryFilter.Value);
        }

        var projected = vendors.Select(v => new
        {
            Vendor = v,
            StartingPrice = _db.VendorOfferings
                .Where(o => o.VendorId == v.Id && o.Price != null)
                .Min(o => (decimal?)o.Price),
            StartingPricingType = _db.VendorOfferings
                .Where(o => o.VendorId == v.Id && o.Price != null)
                .OrderBy(o => o.Price)
                .Select(o => (PricingType?)o.PricingType)
                .FirstOrDefault()
        });

        var descending = string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        projected = query.SortBy?.ToLowerInvariant() switch
        {
            "category" => descending
                ? projected.OrderByDescending(x => x.Vendor.Category).ThenBy(x => x.Vendor.BusinessName)
                : projected.OrderBy(x => x.Vendor.Category).ThenBy(x => x.Vendor.BusinessName),
            "createdat" => descending
                ? projected.OrderByDescending(x => x.Vendor.CreatedAt)
                : projected.OrderBy(x => x.Vendor.CreatedAt),
            "startingprice" => descending
                ? projected.OrderByDescending(x => x.StartingPrice ?? decimal.MinValue)
                : projected.OrderBy(x => x.StartingPrice ?? decimal.MaxValue),
            _ => descending
                ? projected.OrderByDescending(x => x.Vendor.BusinessName)
                : projected.OrderBy(x => x.Vendor.BusinessName)
        };

        var totalCount = await projected.CountAsync();
        var pageItems = await projected
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var items = pageItems.Select(x => new MarketplaceVendorListItemResponse
        {
            Id = x.Vendor.Id,
            BusinessName = x.Vendor.BusinessName,
            Category = x.Vendor.Category.ToString(),
            ShortDescription = Truncate(x.Vendor.Description, 160),
            Address = x.Vendor.Address,
            ProfileImageUrl = x.Vendor.ProfileImageUrl,
            StartingPrice = x.StartingPrice,
            StartingPricingType = x.StartingPricingType?.ToString()
        }).ToList();

        return (items, totalCount);
    }

    public Task<Vendor?> GetApprovedByIdAsync(Guid vendorId) =>
        _db.Vendors
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == vendorId && v.Status == VendorStatus.APPROVED);

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength].TrimEnd() + "…";
    }
}
