using Application.Dtos.Admin;

namespace Application.Common.Interfaces;

public interface IAdminScheduleRepository
{
    Task<(IReadOnlyList<AdminScheduleListItemResponse> Items, int TotalCount)> ListSchedulesAsync(
        AdminScheduleQuery query,
        CancellationToken cancellationToken);

    Task<AdminScheduleDetailResponse?> GetScheduleAsync(Guid scheduleId, CancellationToken cancellationToken);
}
