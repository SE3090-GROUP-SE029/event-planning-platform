using Application.Dtos.Events;

namespace Application.Services.Events;

public interface IAdminEventService
{
    Task<AdminEventListResponse> ListAsync(AdminEventQuery query);
    Task<AdminEventResponse> GetByIdAsync(Guid eventId);
}
