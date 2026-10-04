using Application.Common.Interfaces;
using Application.Dtos.Events;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using FluentValidation.Results;

namespace Application.Services.Events;

public class EventService : IEventService
{
    private readonly IEventRepository _events;
    private readonly IValidator<CreateEventRequest> _validator;

    public EventService(IEventRepository events, IValidator<CreateEventRequest> validator)
    {
        _events = events;
        _validator = validator;
    }

    public async Task<EventResponse> CreateAsync(Guid ownerId, CreateEventRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var now = DateTime.UtcNow;
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            EventName = request.EventName.Trim(),
            EventType = request.EventType!.Value,
            GuestCount = request.GuestCount,
            Budget = request.Budget,
            PreferredVenue = request.PreferredVenue.Trim(),
            PreferredDate = NormalizeEventDate(ResolveEventDate(request)),
            StartTime = request.StartTime!.Value,
            EndTime = request.EndTime!.Value,
            EventDuration = CalculateDuration(request.StartTime.Value, request.EndTime.Value),
            Requirements = string.IsNullOrWhiteSpace(request.Requirements)
                ? null
                : request.Requirements.Trim(),
            Status = EventStatus.DRAFT,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _events.AddAsync(eventEntity);
        await _events.SaveChangesAsync();

        return ToResponse(eventEntity);
    }

    public async Task<EventResponse> GetByIdAsync(Guid eventId, Guid userId, bool isAdmin)
    {
        var eventEntity = await _events.GetByIdAsync(eventId)
            ?? throw new KeyNotFoundException("Event not found.");

        if (!isAdmin && eventEntity.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to access this event.");
        }

        return ToResponse(eventEntity);
    }

    public async Task<EventListResponse> ListAsync(Guid userId, bool isAdmin, EventQuery query)
    {
        ValidateQuery(query);
        var (items, totalCount) = await _events.ListAsync(isAdmin ? null : userId, query);

        return new EventListResponse
        {
            Items = items.Select(ToResponse).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<EventResponse> UpdateAsync(Guid eventId, Guid userId, UpdateEventRequest request)
    {
        var eventEntity = await _events.GetByIdAsync(eventId)
            ?? throw new KeyNotFoundException("Event not found.");

        if (eventEntity.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to update this event.");
        }

        var createRequest = new CreateEventRequest
        {
            EventName = request.EventName,
            EventType = request.EventType,
            GuestCount = request.GuestCount,
            Budget = request.Budget,
            PreferredVenue = request.PreferredVenue,
            EventDate = request.EventDate,
            PreferredDate = request.PreferredDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            EventDuration = eventEntity.EventDuration,
            Requirements = request.Requirements
        };
        await _validator.ValidateAndThrowAsync(createRequest);
        if (!Enum.IsDefined(request.Status))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Status), "Status must be a valid event status.")
            });
        }

        eventEntity.EventType = request.EventType!.Value;
        eventEntity.EventName = request.EventName.Trim();
        eventEntity.GuestCount = request.GuestCount;
        eventEntity.Budget = request.Budget;
        eventEntity.PreferredVenue = request.PreferredVenue.Trim();
        eventEntity.PreferredDate = NormalizeEventDate(ResolveEventDate(request));
        eventEntity.StartTime = request.StartTime!.Value;
        eventEntity.EndTime = request.EndTime!.Value;
        eventEntity.EventDuration = CalculateDuration(eventEntity.StartTime, eventEntity.EndTime);
        eventEntity.Requirements = string.IsNullOrWhiteSpace(request.Requirements) ? null : request.Requirements.Trim();
        eventEntity.Status = request.Status;
        eventEntity.UpdatedAt = DateTime.UtcNow;

        await _events.SaveChangesAsync();
        return ToResponse(eventEntity);
    }

    public async Task DeleteAsync(Guid eventId, Guid userId)
    {
        var eventEntity = await _events.GetByIdAsync(eventId)
            ?? throw new KeyNotFoundException("Event not found.");

        if (eventEntity.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete this event.");
        }

        _events.Remove(eventEntity);
        await _events.SaveChangesAsync();
    }

    private static void ValidateQuery(EventQuery query)
    {
        if (query.Page < 1)
        {
            throw new ArgumentException("Page must be greater than zero.");
        }

        if (query.PageSize is < 1 or > 100)
        {
            throw new ArgumentException("PageSize must be between 1 and 100.");
        }

        if (!new[] { "createdat", "preferreddate", "budget", "guestcount" }
            .Contains(query.SortBy?.ToLowerInvariant()))
        {
            throw new ArgumentException("SortBy must be one of: createdAt, preferredDate, budget, guestCount.");
        }

        if (!new[] { "asc", "desc" }.Contains(query.SortOrder?.ToLowerInvariant()))
        {
            throw new ArgumentException("SortOrder must be asc or desc.");
        }
    }

    private static EventResponse ToResponse(Event eventEntity) => new()
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
        UpdatedAt = eventEntity.UpdatedAt
    };

    private static DateTime ResolveEventDate(CreateEventRequest request) =>
        request.EventDate ?? request.PreferredDate;

    private static DateTime ResolveEventDate(UpdateEventRequest request) =>
        request.EventDate ?? request.PreferredDate;

    private static DateTime NormalizeEventDate(DateTime eventDate) =>
        DateTime.SpecifyKind(eventDate.Date, DateTimeKind.Utc);

    private static TimeSpan CalculateDuration(TimeOnly startTime, TimeOnly endTime) =>
        endTime.ToTimeSpan() - startTime.ToTimeSpan();
}
