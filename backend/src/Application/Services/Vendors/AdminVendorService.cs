using Application.Common.Interfaces;
using Application.Dtos.Vendors;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Vendors;

public sealed class AdminVendorService : IAdminVendorService
{
    private readonly IVendorRepository _vendors;

    public AdminVendorService(IVendorRepository vendors) => _vendors = vendors;

    public async Task<AdminVendorListResponse> ListAsync(AdminVendorQuery query)
    {
        ValidateQuery(query, out var categoryFilter);
        var (items, totalCount) = await _vendors.ListAdminAsync(query, categoryFilter);

        return new AdminVendorListResponse
        {
            Items = items.Select(ToResponse).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<AdminVendorResponse> GetByIdAsync(Guid vendorId)
    {
        var vendor = await _vendors.GetByIdAsync(vendorId)
            ?? throw new KeyNotFoundException("Vendor not found.");
        return ToResponse(vendor);
    }

    public async Task<AdminVendorResponse> ApproveAsync(Guid vendorId)
    {
        var vendor = await RequireVendorAsync(vendorId);

        if (vendor.Status != VendorStatus.PENDING)
        {
            throw new InvalidOperationException("Only PENDING vendors can be approved.");
        }

        vendor.Status = VendorStatus.APPROVED;
        vendor.UpdatedAt = DateTime.UtcNow;
        await _vendors.SaveChangesAsync();
        return ToResponse(vendor);
    }

    public async Task<AdminVendorResponse> SuspendAsync(Guid vendorId)
    {
        var vendor = await RequireVendorAsync(vendorId);

        if (vendor.Status != VendorStatus.APPROVED)
        {
            throw new InvalidOperationException("Only APPROVED vendors can be suspended.");
        }

        vendor.Status = VendorStatus.SUSPEND;
        vendor.UpdatedAt = DateTime.UtcNow;
        await _vendors.SaveChangesAsync();
        return ToResponse(vendor);
    }

    public async Task<AdminVendorResponse> RestoreAsync(Guid vendorId)
    {
        var vendor = await RequireVendorAsync(vendorId);

        if (vendor.Status != VendorStatus.SUSPEND)
        {
            throw new InvalidOperationException("Only SUSPEND vendors can be restored.");
        }

        vendor.Status = VendorStatus.APPROVED;
        vendor.UpdatedAt = DateTime.UtcNow;
        await _vendors.SaveChangesAsync();
        return ToResponse(vendor);
    }

    private async Task<Vendor> RequireVendorAsync(Guid vendorId) =>
        await _vendors.GetByIdAsync(vendorId)
            ?? throw new KeyNotFoundException("Vendor not found.");

    private static void ValidateQuery(AdminVendorQuery query, out BusinessCategory? categoryFilter)
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

        var sortBy = query.SortBy?.ToLowerInvariant();
        if (sortBy is not ("createdat" or "businessname" or "status"))
        {
            throw new ArgumentException("SortBy must be one of: createdAt, businessName, status.");
        }

        if (!new[] { "asc", "desc" }.Contains(query.SortOrder?.ToLowerInvariant()))
        {
            throw new ArgumentException("SortOrder must be asc or desc.");
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            if (!Enum.TryParse<BusinessCategory>(query.Category.Trim(), ignoreCase: true, out var parsed))
            {
                throw new ArgumentException("Invalid category filter.");
            }

            categoryFilter = parsed;
        }
    }

    private static AdminVendorResponse ToResponse(Vendor vendor) => new()
    {
        Id = vendor.Id,
        UserId = vendor.UserId,
        BusinessName = vendor.BusinessName,
        Category = vendor.Category.ToString(),
        ContactEmail = vendor.ContactEmail,
        ContactPhone = vendor.ContactPhone,
        Address = vendor.Address,
        Description = vendor.Description,
        ProfileImageUrl = vendor.ProfileImageUrl,
        WebsiteUrl = vendor.WebsiteUrl,
        Status = vendor.Status.ToString(),
        CreatedAt = vendor.CreatedAt,
        UpdatedAt = vendor.UpdatedAt
    };
}
