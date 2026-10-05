using Application.Common.Interfaces;
using Application.Dtos.Admin;

namespace Application.Services.Admin;

public sealed class AdminScheduleService(IAdminScheduleRepository repository) : IAdminScheduleService
{
    public async Task<AdminScheduleListResponse> ListAsync(
        AdminScheduleQuery query,
        CancellationToken cancellationToken)
    {
        ValidatePaging(query.Page, query.PageSize);
        var (items, total) = await repository.ListSchedulesAsync(query, cancellationToken);
        return new AdminScheduleListResponse
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total,
            TotalPages = Pages(total, query.PageSize)
        };
    }

    public async Task<AdminScheduleDetailResponse> GetByIdAsync(
        Guid scheduleId,
        CancellationToken cancellationToken) =>
        await repository.GetScheduleAsync(scheduleId, cancellationToken)
            ?? throw new KeyNotFoundException("Schedule not found.");

    private static void ValidatePaging(int page, int pageSize)
    {
        if (page < 1) throw new ArgumentException("Page must be greater than zero.");
        if (pageSize is < 1 or > 100) throw new ArgumentException("PageSize must be between 1 and 100.");
    }

    private static int Pages(int total, int pageSize) => total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
}
