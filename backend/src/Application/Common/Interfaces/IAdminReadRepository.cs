using Application.Dtos.Admin;
using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IAdminReadRepository
{
    Task<AdminAnalyticsResponse> GetAnalyticsAsync(DateTime monthStart, CancellationToken cancellationToken);
    Task<(IReadOnlyList<User> Items, int TotalCount)> ListUsersAsync(AdminUserQuery query, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Vendor> Items, int TotalCount)> ListVendorsAsync(AdminVendorQuery query, CancellationToken cancellationToken);
    Task<(IReadOnlyList<EventPlanDraft> Items, int TotalCount)> ListPlansAsync(AdminPlanQuery query, CancellationToken cancellationToken);
    Task<EventPlanDraft?> GetPlanAsync(Guid id, CancellationToken cancellationToken);
}
