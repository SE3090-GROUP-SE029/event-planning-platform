using Application.Common.Interfaces;
using Application.Dtos.Vendors;
using Domain.Entities;

namespace Application.Services.Vendors;

public class VendorAvailabilityService : IVendorAvailabilityService
{
    private readonly IVendorRepository _vendors;
    private readonly IVendorAvailabilityRepository _availability;

    public VendorAvailabilityService(IVendorRepository vendors, IVendorAvailabilityRepository availability)
    {
        _vendors = vendors;
        _availability = availability;
    }

    public async Task<VendorAvailabilityResponse> CreateAsync(Guid userId, CreateVendorAvailabilityRequest request)
    {
        var vendor = await RequireVendorAsync(userId);
        var (start, end) = NormalizeRange(request.StartDateTime, request.EndDateTime);
        await EnsureNoOverlapAsync(vendor.Id, start, end);

        var item = new VendorAvailability
        {
            Id = Guid.NewGuid(),
            VendorId = vendor.Id,
            StartDateTime = start,
            EndDateTime = end,
            IsAvailable = request.IsAvailable,
            CreatedAt = DateTime.UtcNow
        };

        await _availability.AddAsync(item);
        await _availability.SaveChangesAsync();
        return ToResponse(item);
    }

    public async Task<IReadOnlyList<VendorAvailabilityResponse>> ListMineAsync(Guid userId)
    {
        var vendor = await RequireVendorAsync(userId);
        var items = await _availability.ListByVendorIdAsync(vendor.Id);
        return items.Select(ToResponse).ToList();
    }

    public async Task<VendorAvailabilityResponse> UpdateAsync(
        Guid userId,
        Guid availabilityId,
        UpdateVendorAvailabilityRequest request)
    {
        var vendor = await RequireVendorAsync(userId);
        var item = await RequireOwnedAsync(vendor.Id, availabilityId);
        var (start, end) = NormalizeRange(request.StartDateTime, request.EndDateTime);
        await EnsureNoOverlapAsync(vendor.Id, start, end, excludeId: item.Id);

        item.StartDateTime = start;
        item.EndDateTime = end;
        item.IsAvailable = request.IsAvailable;
        item.UpdatedAt = DateTime.UtcNow;

        await _availability.SaveChangesAsync();
        return ToResponse(item);
    }

    public async Task DeleteAsync(Guid userId, Guid availabilityId)
    {
        var vendor = await RequireVendorAsync(userId);
        var item = await RequireOwnedAsync(vendor.Id, availabilityId);
        _availability.Remove(item);
        await _availability.SaveChangesAsync();
    }

    private async Task<Vendor> RequireVendorAsync(Guid userId)
    {
        return await _vendors.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Create a vendor profile before managing availability.");
    }

    private async Task<VendorAvailability> RequireOwnedAsync(Guid vendorId, Guid availabilityId)
    {
        var item = await _availability.GetByIdAsync(availabilityId)
            ?? throw new KeyNotFoundException("Vendor availability period not found.");

        if (item.VendorId != vendorId)
        {
            throw new UnauthorizedAccessException("You can only manage your own vendor availability.");
        }

        return item;
    }

    private async Task EnsureNoOverlapAsync(Guid vendorId, DateTime start, DateTime end, Guid? excludeId = null)
    {
        if (await _availability.HasOverlapAsync(vendorId, start, end, excludeId))
        {
            throw new ArgumentException("This period overlaps an existing availability period for your business.");
        }
    }

    private static (DateTime Start, DateTime End) NormalizeRange(DateTime start, DateTime end)
    {
        var normalizedStart = ToUtc(start);
        var normalizedEnd = ToUtc(end);

        if (normalizedStart >= normalizedEnd)
        {
            throw new ArgumentException("Start date/time must be before end date/time.");
        }

        return (normalizedStart, normalizedEnd);
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static VendorAvailabilityResponse ToResponse(VendorAvailability item) => new()
    {
        Id = item.Id,
        VendorId = item.VendorId,
        StartDateTime = item.StartDateTime,
        EndDateTime = item.EndDateTime,
        IsAvailable = item.IsAvailable,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt
    };
}
