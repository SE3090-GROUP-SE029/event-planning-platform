using Application.Dtos.Events;

namespace Application.Services.Events;

public interface IEventService
{
    Task<EventResponse> CreateAsync(Guid ownerId, CreateEventRequest request);
    Task<EventResponse> GetByIdAsync(Guid eventId, Guid userId, bool isAdmin);
    Task<EventListResponse> ListAsync(Guid userId, bool isAdmin, EventQuery query);
    Task<EventResponse> UpdateAsync(Guid eventId, Guid userId, UpdateEventRequest request);
    Task DeleteAsync(Guid eventId, Guid userId);
}
