using Application.Common.Interfaces;
using Application.Dtos.Vendors;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories;

public class VendorMarketplaceRepository : IVendorMarketplaceRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<VendorMarketplaceRepository> _logger;

    public VendorMarketplaceRepository(AppDbContext db, ILogger<VendorMarketplaceRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<(IReadOnlyList<MarketplaceVendorListItemResponse> Items, int TotalCount)> SearchApprovedAsync(
        VendorMarketplaceQuery query,
        BusinessCategory? categoryFilter)
    {
        var allVendors = _db.Vendors.AsNoTracking();
        var approvedVendors = allVendors.Where(v => v.Status == VendorStatus.APPROVED);
        var vendors = approvedVendors;

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            vendors = vendors.Where(v => v.BusinessName.ToLower().Contains(search));
        }
        var afterSearch = vendors;

        if (categoryFilter.HasValue)
        {
            vendors = vendors.Where(v => v.Category == categoryFilter.Value);
        }

        var descending = string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        vendors = query.SortBy?.ToLowerInvariant() switch
        {
            "category" => descending
                ? vendors.OrderByDescending(v => v.Category).ThenBy(v => v.BusinessName)
                : vendors.OrderBy(v => v.Category).ThenBy(v => v.BusinessName),
            "createdat" => descending
                ? vendors.OrderByDescending(v => v.CreatedAt).ThenBy(v => v.BusinessName)
                : vendors.OrderBy(v => v.CreatedAt).ThenBy(v => v.BusinessName),
            "startingprice" => descending
                ? vendors
                    .OrderByDescending(v => _db.VendorOfferings
                        .Where(o => o.VendorId == v.Id && o.Price != null)
                        .Min(o => (decimal?)o.Price) ?? decimal.MinValue)
                    .ThenBy(v => v.BusinessName)
                : vendors
                    .OrderBy(v => _db.VendorOfferings
                        .Where(o => o.VendorId == v.Id && o.Price != null)
                        .Min(o => (decimal?)o.Price) ?? decimal.MaxValue)
                    .ThenBy(v => v.BusinessName),
            _ => descending
                ? vendors.OrderByDescending(v => v.BusinessName)
                : vendors.OrderBy(v => v.BusinessName)
        };

        var totalCount = await vendors.CountAsync();
        var pageVendors = await vendors
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        if (totalCount == 0)
        {
            var databaseCount = await allVendors.CountAsync();
            var approvedCount = await approvedVendors.CountAsync();
            var afterSearchCount = await afterSearch.CountAsync();
            _logger.LogInformation(
                "Vendor marketplace returned no matches. DatabaseVendors={DatabaseVendors}, " +
                "ApprovedVendors={ApprovedVendors}, AfterSearch={AfterSearch}, " +
                "AfterCategory={AfterCategory}, Page={Page}, PageSize={PageSize}. " +
                "Marketplace list does not filter vendors by service availability.",
                databaseCount,
                approvedCount,
                afterSearchCount,
                totalCount,
                query.Page,
                query.PageSize);
        }
        else if (pageVendors.Count == 0)
        {
            _logger.LogInformation(
                "Vendor marketplace page is empty although matches exist. " +
                "FilteredVendors={FilteredVendors}, Page={Page}, PageSize={PageSize}.",
                totalCount,
                query.Page,
                query.PageSize);
        }

        if (pageVendors.Count == 0)
        {
            return ([], totalCount);
        }

        var vendorIds = pageVendors.Select(v => v.Id).ToList();
        var offeringRows = await _db.VendorOfferings
            .AsNoTracking()
            .Where(o => vendorIds.Contains(o.VendorId) && o.Price != null)
            .OrderBy(o => o.Price)
            .Select(o => new
            {
                o.VendorId,
                o.Price,
                o.PricingType
            })
            .ToListAsync();
        var lowestOfferings = offeringRows
            .GroupBy(o => o.VendorId)
            .ToDictionary(g => g.Key, g => g.First());

        var items = pageVendors.Select(v =>
        {
            lowestOfferings.TryGetValue(v.Id, out var offering);
            return new MarketplaceVendorListItemResponse
            {
                Id = v.Id,
                BusinessName = v.BusinessName,
                Category = v.Category.ToString(),
                ShortDescription = Truncate(v.Description, 160),
                Address = v.Address,
                ProfileImageUrl = v.ProfileImageUrl,
                StartingPrice = offering?.Price,
                StartingPricingType = offering?.PricingType?.ToString()
            };
        }).ToList();

        var aggregates = await _db.VendorRatings
            .AsNoTracking()
            .Where(r => vendorIds.Contains(r.VendorId))
            .GroupBy(r => r.VendorId)
            .Select(g => new
            {
                VendorId = g.Key,
                Average = g.Average(r => (decimal)r.Rating),
                Count = g.Count()
            })
            .ToListAsync();

        var byVendor = aggregates.ToDictionary(a => a.VendorId);
        foreach (var item in items)
        {
            if (byVendor.TryGetValue(item.Id, out var stats))
            {
                item.AverageRating = Math.Round(stats.Average, 2, MidpointRounding.AwayFromZero);
                item.ReviewCount = stats.Count;
            }
        }

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
