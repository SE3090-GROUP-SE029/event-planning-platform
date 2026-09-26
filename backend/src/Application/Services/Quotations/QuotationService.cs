using Application.Common.Interfaces;
using Application.Dtos.Quotations;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Quotations;

public class QuotationService : IQuotationService
{
    private readonly IQuotationRepository _quotations;
    private readonly IVendorRepository _vendors;
    private readonly IVendorOfferingRepository _offerings;
    private readonly IVendorAvailabilityRepository _availability;
    private readonly IEventRepository _events;

    public QuotationService(
        IQuotationRepository quotations,
        IVendorRepository vendors,
        IVendorOfferingRepository offerings,
        IVendorAvailabilityRepository availability,
        IEventRepository events)
    {
        _quotations = quotations;
        _vendors = vendors;
        _offerings = offerings;
        _availability = availability;
        _events = events;
    }

    public async Task<QuotationResponse> CreateAsync(Guid requestedByUserId, CreateQuotationRequest request)
    {
        var (start, end) = NormalizeRange(request.RequestedStartDateTime, request.RequestedEndDateTime);

        var eventEntity = await _events.GetByIdAsync(request.EventId)
            ?? throw new KeyNotFoundException("Event not found.");

        if (eventEntity.OwnerId != requestedByUserId)
        {
            throw new UnauthorizedAccessException("You can only request quotations for your own events.");
        }

        var vendor = await _vendors.GetByIdAsync(request.VendorId)
            ?? throw new KeyNotFoundException("Vendor not found.");

        if (vendor.Status != VendorStatus.APPROVED)
        {
            throw new ArgumentException("Quotations can only be requested from approved vendors.");
        }

        var offering = await _offerings.GetByIdAsync(request.VendorServiceId)
            ?? throw new KeyNotFoundException("Vendor service not found.");

        if (offering.VendorId != vendor.Id)
        {
            throw new ArgumentException("The selected service does not belong to this vendor.");
        }

        await EnsureAvailableForPeriodAsync(vendor.Id, start, end);

        var now = DateTime.UtcNow;
        var quotation = new Quotation
        {
            Id = Guid.NewGuid(),
            EventId = eventEntity.Id,
            VendorId = vendor.Id,
            VendorServiceId = offering.Id,
            RequestedByUserId = requestedByUserId,
            RequestedStartDateTime = start,
            RequestedEndDateTime = end,
            CustomerMessage = string.IsNullOrWhiteSpace(request.CustomerMessage)
                ? null
                : request.CustomerMessage.Trim(),
            Status = QuotationStatus.REQUESTED,
            RequestedAt = now
        };

        await _quotations.AddAsync(quotation);
        await _quotations.SaveChangesAsync();

        return await ToResponseAsync(quotation, vendor, offering, eventEntity);
    }

    public async Task<IReadOnlyList<QuotationResponse>> ListMineAsync(Guid requestedByUserId)
    {
        var items = await _quotations.ListByRequesterAsync(requestedByUserId);
        return await MapManyAsync(items);
    }

    public async Task<IReadOnlyList<QuotationResponse>> ListForVendorAsync(Guid vendorUserId)
    {
        var vendor = await RequireVendorAsync(vendorUserId);
        var items = await _quotations.ListByVendorIdAsync(vendor.Id);
        return await MapManyAsync(items);
    }

    public async Task<QuotationResponse> GetByIdAsync(Guid quotationId, Guid userId, bool isVendor)
    {
        var quotation = await _quotations.GetByIdAsync(quotationId)
            ?? throw new KeyNotFoundException("Quotation not found.");

        if (quotation.RequestedByUserId == userId)
        {
            return await ToResponseAsync(quotation);
        }

        if (isVendor)
        {
            var vendor = await _vendors.GetByUserIdAsync(userId);
            if (vendor != null && vendor.Id == quotation.VendorId)
            {
                return await ToResponseAsync(quotation);
            }
        }

        throw new UnauthorizedAccessException("You are not authorized to view this quotation.");
    }

    public async Task<QuotationResponse> RespondAsync(
        Guid vendorUserId,
        Guid quotationId,
        RespondToQuotationRequest request)
    {
        var vendor = await RequireVendorAsync(vendorUserId);
        var quotation = await _quotations.GetByIdAsync(quotationId)
            ?? throw new KeyNotFoundException("Quotation not found.");

        if (quotation.VendorId != vendor.Id)
        {
            throw new UnauthorizedAccessException("You can only respond to quotations for your own business.");
        }

        if (quotation.Status != QuotationStatus.REQUESTED)
        {
            throw new InvalidOperationException("Only pending (REQUESTED) quotations can be responded to.");
        }

        if (request.QuotedPrice < 0)
        {
            throw new ArgumentException("Quoted price cannot be negative.");
        }

        quotation.QuotedPrice = request.QuotedPrice;
        quotation.VendorTerms = string.IsNullOrWhiteSpace(request.VendorTerms)
            ? null
            : request.VendorTerms.Trim();
        quotation.Status = QuotationStatus.QUOTED;
        quotation.RespondedAt = DateTime.UtcNow;
        quotation.UpdatedAt = DateTime.UtcNow;

        await _quotations.SaveChangesAsync();
        return await ToResponseAsync(quotation, vendor);
    }

    private async Task EnsureAvailableForPeriodAsync(Guid vendorId, DateTime start, DateTime end)
    {
        var windows = await _availability.ListByVendorIdAsync(vendorId);

        var blocked = windows.Any(w =>
            !w.IsAvailable
            && w.StartDateTime < end
            && start < w.EndDateTime);

        if (blocked)
        {
            throw new ArgumentException("The vendor is marked unavailable for part of the requested period.");
        }

        var covered = windows.Any(w =>
            w.IsAvailable
            && w.StartDateTime <= start
            && end <= w.EndDateTime);

        if (!covered)
        {
            throw new ArgumentException(
                "The requested period is not fully covered by the vendor's available schedule.");
        }
    }

    private async Task<Vendor> RequireVendorAsync(Guid userId)
    {
        return await _vendors.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Create a vendor profile before managing quotations.");
    }

    private async Task<IReadOnlyList<QuotationResponse>> MapManyAsync(IReadOnlyList<Quotation> items)
    {
        var results = new List<QuotationResponse>(items.Count);
        foreach (var item in items)
        {
            results.Add(await ToResponseAsync(item));
        }

        return results;
    }

    private async Task<QuotationResponse> ToResponseAsync(
        Quotation quotation,
        Vendor? vendor = null,
        VendorOffering? offering = null,
        Event? eventEntity = null)
    {
        vendor ??= await _vendors.GetByIdAsync(quotation.VendorId);
        offering ??= await _offerings.GetByIdAsync(quotation.VendorServiceId);
        eventEntity ??= await _events.GetByIdAsync(quotation.EventId);

        return new QuotationResponse
        {
            Id = quotation.Id,
            EventId = quotation.EventId,
            EventType = eventEntity?.EventType.ToString(),
            GuestCount = eventEntity?.GuestCount,
            EventPreferredDate = eventEntity?.PreferredDate,
            EventRequirements = eventEntity?.Requirements,
            VendorId = quotation.VendorId,
            VendorBusinessName = vendor?.BusinessName ?? "Unknown vendor",
            VendorServiceId = quotation.VendorServiceId,
            ServiceName = offering?.ServiceName ?? "Unknown service",
            RequestedByUserId = quotation.RequestedByUserId,
            RequestedStartDateTime = quotation.RequestedStartDateTime,
            RequestedEndDateTime = quotation.RequestedEndDateTime,
            CustomerMessage = quotation.CustomerMessage,
            QuotedPrice = quotation.QuotedPrice,
            VendorTerms = quotation.VendorTerms,
            Status = quotation.Status.ToString(),
            RequestedAt = quotation.RequestedAt,
            RespondedAt = quotation.RespondedAt,
            UpdatedAt = quotation.UpdatedAt
        };
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

    private static DateTime ToUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
