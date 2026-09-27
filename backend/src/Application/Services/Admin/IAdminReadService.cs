using Application.Dtos.Admin;

namespace Application.Services.Admin;

public interface IAdminReadService
{
    Task<AdminAnalyticsResponse> GetAnalyticsAsync(CancellationToken cancellationToken);
    Task<AdminUserListResponse> ListUsersAsync(AdminUserQuery query, CancellationToken cancellationToken);
    Task<AdminVendorListResponse> ListVendorsAsync(AdminVendorQuery query, CancellationToken cancellationToken);
    Task<AdminPlanListResponse> ListPlansAsync(AdminPlanQuery query, CancellationToken cancellationToken);
    Task<AdminPlanResponse> GetPlanAsync(Guid id, CancellationToken cancellationToken);
}
