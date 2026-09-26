using Application.Common.Interfaces;
using Application.Dtos.Vendors;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Vendors;

public class VendorOfferingService : IVendorOfferingService
{
    private readonly IVendorRepository _vendors;
    private readonly IVendorOfferingRepository _offerings;

    public VendorOfferingService(IVendorRepository vendors, IVendorOfferingRepository offerings)
    {
        _vendors = vendors;
        _offerings = offerings;
    }

    public async Task<VendorOfferingResponse> CreateAsync(Guid userId, CreateVendorOfferingRequest request)
    {
        var vendor = await RequireVendorAsync(userId);
        ValidateFields(request.ServiceName, request.Description);
        var (price, pricingType) = NormalizePricing(request.Price, request.PricingType);

        var offering = new VendorOffering
        {
            Id = Guid.NewGuid(),
            VendorId = vendor.Id,
            ServiceName = request.ServiceName.Trim(),
            Description = NormalizeDescription(request.Description),
            Price = price,
            PricingType = pricingType,
            CreatedAt = DateTime.UtcNow
        };

        await _offerings.AddAsync(offering);
        await _offerings.SaveChangesAsync();
        return ToResponse(offering);
    }

    public async Task<IReadOnlyList<VendorOfferingResponse>> ListMineAsync(Guid userId)
    {
        var vendor = await RequireVendorAsync(userId);
        var items = await _offerings.ListByVendorIdAsync(vendor.Id);
        return items.Select(ToResponse).ToList();
    }

    public async Task<VendorOfferingResponse> UpdateAsync(Guid userId, Guid offeringId, UpdateVendorOfferingRequest request)
    {
        var vendor = await RequireVendorAsync(userId);
        var offering = await RequireOwnedOfferingAsync(vendor.Id, offeringId);
        ValidateFields(request.ServiceName, request.Description);
        var (price, pricingType) = NormalizePricing(request.Price, request.PricingType);

        offering.ServiceName = request.ServiceName.Trim();
        offering.Description = NormalizeDescription(request.Description);
        offering.Price = price;
        offering.PricingType = pricingType;
        offering.UpdatedAt = DateTime.UtcNow;

        await _offerings.SaveChangesAsync();
        return ToResponse(offering);
    }

    public async Task DeleteAsync(Guid userId, Guid offeringId)
    {
        var vendor = await RequireVendorAsync(userId);
        var offering = await RequireOwnedOfferingAsync(vendor.Id, offeringId);
        _offerings.Remove(offering);
        await _offerings.SaveChangesAsync();
    }

    private async Task<Vendor> RequireVendorAsync(Guid userId)
    {
        return await _vendors.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Create a vendor profile before managing services.");
    }

    private async Task<VendorOffering> RequireOwnedOfferingAsync(Guid vendorId, Guid offeringId)
    {
        var offering = await _offerings.GetByIdAsync(offeringId)
            ?? throw new KeyNotFoundException("Vendor service not found.");

        if (offering.VendorId != vendorId)
        {
            throw new UnauthorizedAccessException("You can only manage your own vendor services.");
        }

        return offering;
    }

    private static void ValidateFields(string? serviceName, string? description)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            throw new ArgumentException("Service name is required.");
        }

        if (serviceName.Trim().Length > 200)
        {
            throw new ArgumentException("Service name must be 200 characters or fewer.");
        }

        if (description is not null && description.Trim().Length > 2000)
        {
            throw new ArgumentException("Description must be 2000 characters or fewer.");
        }
    }

    private static (decimal? Price, PricingType? PricingType) NormalizePricing(decimal? price, string? pricingType)
    {
        var hasPrice = price.HasValue;
        var hasType = !string.IsNullOrWhiteSpace(pricingType);

        if (!hasPrice && !hasType)
        {
            return (null, null);
        }

        if (hasPrice && !hasType)
        {
            throw new ArgumentException("Pricing type is required when a price is set.");
        }

        if (!hasPrice && hasType)
        {
            throw new ArgumentException("Price is required when a pricing type is set. Omit both to clear pricing.");
        }

        if (price < 0)
        {
            throw new ArgumentException("Price must be zero or greater.");
        }

        if (!Enum.TryParse<PricingType>(pricingType!.Trim(), true, out var parsed))
        {
            throw new ArgumentException("Pricing type must be one of: FIXED, PER_PERSON, PER_HOUR, PER_DAY.");
        }

        return (decimal.Round(price!.Value, 2, MidpointRounding.AwayFromZero), parsed);
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return description.Trim();
    }

    private static VendorOfferingResponse ToResponse(VendorOffering offering) => new()
    {
        Id = offering.Id,
        VendorId = offering.VendorId,
        ServiceName = offering.ServiceName,
        Description = offering.Description,
        Price = offering.Price,
        PricingType = offering.PricingType?.ToString(),
        CreatedAt = offering.CreatedAt,
        UpdatedAt = offering.UpdatedAt
    };
}
