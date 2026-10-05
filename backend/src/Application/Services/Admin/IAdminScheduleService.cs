using Application.Dtos.Admin;

namespace Application.Services.Admin;

public interface IAdminScheduleService
{
    Task<AdminScheduleListResponse> ListAsync(AdminScheduleQuery query, CancellationToken cancellationToken);
    Task<AdminScheduleDetailResponse> GetByIdAsync(Guid scheduleId, CancellationToken cancellationToken);
}
