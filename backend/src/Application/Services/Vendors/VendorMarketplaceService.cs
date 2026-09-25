using Application.Common.Interfaces;
using Application.Dtos.Vendors;
using Domain.Enums;

namespace Application.Services.Vendors;

public class VendorMarketplaceService : IVendorMarketplaceService
{
    private const int UpcomingAvailabilityLimit = 20;

    private readonly IVendorMarketplaceRepository _marketplace;
    private readonly IVendorOfferingRepository _offerings;
    private readonly IVendorGalleryImageRepository _images;
    private readonly IVendorAvailabilityRepository _availability;

    public VendorMarketplaceService(
        IVendorMarketplaceRepository marketplace,
        IVendorOfferingRepository offerings,
        IVendorGalleryImageRepository images,
        IVendorAvailabilityRepository availability)
    {
        _marketplace = marketplace;
        _offerings = offerings;
        _images = images;
        _availability = availability;
    }

    public async Task<MarketplaceVendorListResponse> ListAsync(VendorMarketplaceQuery query)
    {
        ValidateQuery(query, out var categoryFilter);
        var (items, totalCount) = await _marketplace.SearchApprovedAsync(query, categoryFilter);

        return new MarketplaceVendorListResponse
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<MarketplaceVendorDetailResponse> GetByIdAsync(Guid vendorId)
    {
        var vendor = await _marketplace.GetApprovedByIdAsync(vendorId)
            ?? throw new KeyNotFoundException("Vendor not found.");

        var services = await _offerings.ListByVendorIdAsync(vendor.Id);
        var images = await _images.ListByVendorIdAsync(vendor.Id);
        var availability = await _availability.ListByVendorIdAsync(vendor.Id);
        var now = DateTime.UtcNow;

        return new MarketplaceVendorDetailResponse
        {
            Id = vendor.Id,
            BusinessName = vendor.BusinessName,
            Category = vendor.Category.ToString(),
            Description = vendor.Description,
            Address = vendor.Address,
            ProfileImageUrl = vendor.ProfileImageUrl,
            WebsiteUrl = vendor.WebsiteUrl,
            Images = images.Select(i => new MarketplaceGalleryImageResponse
            {
                Id = i.Id,
                ImageUrl = i.ImageUrl
            }).ToList(),
            Services = services.Select(s => new MarketplaceServiceItemResponse
            {
                Id = s.Id,
                ServiceName = s.ServiceName,
                Description = s.Description,
                Price = s.Price,
                PricingType = s.PricingType?.ToString()
            }).ToList(),
            Availability = availability
                .Where(a => a.EndDateTime >= now)
                .Take(UpcomingAvailabilityLimit)
                .Select(a => new MarketplaceAvailabilityItemResponse
                {
                    Id = a.Id,
                    StartDateTime = a.StartDateTime,
                    EndDateTime = a.EndDateTime,
                    IsAvailable = a.IsAvailable
                }).ToList()
        };
    }

    private static void ValidateQuery(VendorMarketplaceQuery query, out BusinessCategory? categoryFilter)
    {
        categoryFilter = null;

        if (query.Page < 1)
        {
            throw new ArgumentException("Page must be greater than zero.");
        }

        if (query.PageSize is < 1 or > 100)
        {
            throw new ArgumentException("PageSize must be between 1 and 100.");
        }

        var sortBy = string.IsNullOrWhiteSpace(query.SortBy) ? "businessName" : query.SortBy.Trim();
        var sortOrder = string.IsNullOrWhiteSpace(query.SortOrder) ? "asc" : query.SortOrder.Trim();
        query.SortBy = sortBy;
        query.SortOrder = sortOrder;

        if (!new[] { "businessname", "category", "createdat", "startingprice" }
            .Contains(sortBy.ToLowerInvariant()))
        {
            throw new ArgumentException("SortBy must be one of: businessName, category, createdAt, startingPrice.");
        }

        if (!new[] { "asc", "desc" }.Contains(sortOrder.ToLowerInvariant()))
        {
            throw new ArgumentException("SortOrder must be asc or desc.");
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            if (!Enum.TryParse<BusinessCategory>(query.Category.Trim(), true, out var parsed))
            {
                throw new ArgumentException(
                    "Category must be one of: CATERING, PHOTOGRAPHY, VENUE, MUSIC, FLORIST, TRANSPORTATION.");
            }

            categoryFilter = parsed;
        }
    }
}
