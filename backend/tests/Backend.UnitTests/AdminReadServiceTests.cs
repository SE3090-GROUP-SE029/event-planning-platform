using Application.Common.Interfaces;
using Application.Dtos.Admin;
using Application.Services.Admin;
using Domain.Entities;
using Domain.Enums;

namespace Backend.UnitTests;

public class AdminReadServiceTests
{
    [Fact]
    public async Task ListUsersAsync_preserves_real_roles_and_paging_metadata()
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Email = "planner@example.com", FirstName = "Event",
            LastName = "Planner", CreatedAt = DateTime.UtcNow, IsActive = true,
            UserRoles = [new UserRole
            {
                Role = new Role { RoleName = RoleName.EVENT_PLANNER }
            }]
        };
        var repository = new FakeAdminReadRepository { Users = [user] };
        var result = await new AdminReadService(repository).ListUsersAsync(
            new AdminUserQuery { Page = 2, PageSize = 5 }, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(["EVENT_PLANNER"], result.Items[0].Roles);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task GetAnalyticsAsync_returns_repository_metrics_without_client_side_inference()
    {
        var expected = new AdminAnalyticsResponse
        {
            Users = new UserAnalytics { Total = 4, Active = 3, EventPlanners = 2, Vendors = 1, Administrators = 1, RegistrationsThisMonth = 2 },
            Events = new EventAnalytics { Total = 7, Active = 2, Completed = 3, Cancelled = 1, CreatedThisMonth = 4 },
            AiPlans = new AiPlanAnalytics { Generated = 5, Approved = 2, Rejected = 1, Pending = 2, ApprovalRate = 66.67m, GeneratedThisMonth = 2 },
            Vendors = new VendorAnalytics { Total = 3, Approved = 2, Pending = 1 }
        };
        var repository = new FakeAdminReadRepository { Analytics = expected };

        var result = await new AdminReadService(repository).GetAnalyticsAsync(CancellationToken.None);

        Assert.Equal(4, result.Users.Total);
        Assert.Equal(66.67m, result.AiPlans.ApprovalRate);
        Assert.Equal(3, result.Vendors.Total);
    }

    [Fact]
    public async Task ListPlansAsync_normalizes_date_filters_before_repository_query()
    {
        var repository = new FakeAdminReadRepository();
        var service = new AdminReadService(repository);
        var dateFrom = new DateTime(2026, 10, 4);
        var dateTo = new DateTime(2026, 10, 5);

        await service.ListPlansAsync(new AdminPlanQuery
        {
            DateFrom = dateFrom,
            DateTo = dateTo
        }, CancellationToken.None);

        Assert.Equal(DateTimeKind.Utc, repository.LastPlanQuery!.DateFrom!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, repository.LastPlanQuery.DateTo!.Value.Kind);
        Assert.Equal(dateFrom.Ticks, repository.LastPlanQuery.DateFrom.Value.Ticks);
        Assert.Equal(dateTo.Ticks, repository.LastPlanQuery.DateTo.Value.Ticks);
    }

    [Fact]
    public async Task ListPlansAsync_rejects_reversed_date_range()
    {
        var service = new AdminReadService(new FakeAdminReadRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => service.ListPlansAsync(
            new AdminPlanQuery
            {
                DateFrom = new DateTime(2026, 10, 5),
                DateTo = new DateTime(2026, 10, 4)
            },
            CancellationToken.None));
    }

    private sealed class FakeAdminReadRepository : IAdminReadRepository
    {
        public IReadOnlyList<User> Users { get; init; } = [];
        public AdminAnalyticsResponse Analytics { get; init; } = new();
        public AdminPlanQuery? LastPlanQuery { get; private set; }

        public Task<AdminAnalyticsResponse> GetAnalyticsAsync(DateTime monthStart, CancellationToken cancellationToken) =>
            Task.FromResult(Analytics);

        public Task<(IReadOnlyList<User> Items, int TotalCount)> ListUsersAsync(AdminUserQuery query, CancellationToken cancellationToken) =>
            Task.FromResult((Users, Users.Count));

        public Task<(IReadOnlyList<Vendor> Items, int TotalCount)> ListVendorsAsync(AdminVendorQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(((IReadOnlyList<Vendor>)[], 0));

        public Task<(IReadOnlyList<EventPlanDraft> Items, int TotalCount)> ListPlansAsync(AdminPlanQuery query, CancellationToken cancellationToken)
        {
            LastPlanQuery = query;
            return Task.FromResult(((IReadOnlyList<EventPlanDraft>)[], 0));
        }

        public Task<EventPlanDraft?> GetPlanAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<EventPlanDraft?>(null);
    }
}
