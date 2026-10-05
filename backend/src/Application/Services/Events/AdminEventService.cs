using Application.Common;
using Application.Common.Interfaces;
using Application.Dtos.Events;
using Microsoft.Extensions.Logging;

namespace Application.Services.Events;

public sealed class AdminEventService : IAdminEventService
{
    private readonly IEventRepository _events;
    private readonly IUserRepository _users;
    private readonly ILogger<AdminEventService> _logger;

    public AdminEventService(
        IEventRepository events,
        IUserRepository users,
        ILogger<AdminEventService> logger)
    {
        _events = events;
        _users = users;
        _logger = logger;
    }

    public async Task<AdminEventListResponse> ListAsync(AdminEventQuery query)
    {
        if (query.DateFrom.HasValue)
            query.DateFrom = UtcDateTime.Normalize(query.DateFrom.Value);
        if (query.DateTo.HasValue)
            query.DateTo = UtcDateTime.Normalize(query.DateTo.Value);

        ValidateQuery(query);
        _logger.LogInformation(
            "Listing admin events. Page={Page}, PageSize={PageSize}, Status={Status}, EventType={EventType}, HasSearch={HasSearch}, DateFrom={DateFrom}, DateTo={DateTo}",
            query.Page,
            query.PageSize,
            query.Status,
            query.EventType,
            !string.IsNullOrWhiteSpace(query.Search),
            query.DateFrom,
            query.DateTo);

        var (items, totalCount) = await _events.ListAdminAsync(query);
        var owners = await _users.GetByIdsAsync(items.Select(item => item.OwnerId).Distinct());
        var ownersById = owners.ToDictionary(owner => owner.Id);

        return new AdminEventListResponse
        {
            Items = items.Select(item => ToResponse(item, ownersById.GetValueOrDefault(item.OwnerId))).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<AdminEventResponse> GetByIdAsync(Guid eventId)
    {
        var eventEntity = await _events.GetByIdAsync(eventId)
            ?? throw new KeyNotFoundException("Event not found.");
        var owner = await _users.GetByIdsAsync([eventEntity.OwnerId]);
        return ToResponse(eventEntity, owner.FirstOrDefault());
    }

    private static void ValidateQuery(AdminEventQuery query)
    {
        if (query.Page < 1) throw new ArgumentException("Page must be greater than zero.");
        if (query.PageSize is < 1 or > 100) throw new ArgumentException("PageSize must be between 1 and 100.");
        if (query.Search?.Length > 200) throw new ArgumentException("Search must be 200 characters or fewer.");
        if (!new[] { "createdat", "preferreddate", "budget" }.Contains(query.SortBy?.ToLowerInvariant()))
            throw new ArgumentException("SortBy must be one of: createdAt, preferredDate, budget.");
        if (!new[] { "asc", "desc" }.Contains(query.SortOrder?.ToLowerInvariant()))
            throw new ArgumentException("SortOrder must be asc or desc.");
        if (query.DateFrom.HasValue && query.DateTo.HasValue && query.DateFrom > query.DateTo)
            throw new ArgumentException("DateFrom must be earlier than or equal to DateTo.");
    }

    private static AdminEventResponse ToResponse(Domain.Entities.Event eventEntity, Domain.Entities.User? owner) => new()
    {
        Id = eventEntity.Id,
        OwnerId = eventEntity.OwnerId,
        EventName = eventEntity.EventName,
        EventType = eventEntity.EventType,
        GuestCount = eventEntity.GuestCount,
        Budget = eventEntity.Budget,
        PreferredVenue = eventEntity.PreferredVenue!,
        EventDate = eventEntity.PreferredDate.Date,
        PreferredDate = eventEntity.PreferredDate,
        StartTime = eventEntity.StartTime,
        EndTime = eventEntity.EndTime,
        EventDuration = eventEntity.EventDuration,
        Requirements = eventEntity.Requirements,
        Status = eventEntity.Status,
        CreatedAt = eventEntity.CreatedAt,
        UpdatedAt = eventEntity.UpdatedAt,
        Owner = owner is null ? null : new AdminOwnerResponse
        {
            Id = owner.Id,
            Email = owner.Email,
            FirstName = owner.FirstName,
            LastName = owner.LastName
        }
    };
}
