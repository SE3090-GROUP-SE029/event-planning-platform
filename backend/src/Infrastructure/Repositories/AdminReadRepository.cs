using Application.Common.Interfaces;
using Application.Dtos.Admin;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class AdminReadRepository(AppDbContext db) : IAdminReadRepository
{
    public async Task<AdminAnalyticsResponse> GetAnalyticsAsync(
        DateTime monthStart,
        CancellationToken cancellationToken)
    {
        var users = await db.Users.AsNoTracking().Include(user => user.UserRoles).ThenInclude(userRole => userRole.Role)
            .ToListAsync(cancellationToken);
        var events = await db.Events.AsNoTracking().Select(eventEntity => new { eventEntity.Status, eventEntity.CreatedAt })
            .ToListAsync(cancellationToken);
        var plans = await db.EventPlanDrafts.AsNoTracking()
            .Select(plan => new { plan.Status, plan.GeneratedAt }).ToListAsync(cancellationToken);
        var vendors = await db.Vendors.AsNoTracking()
            .Select(vendor => new { vendor.Status, vendor.CreatedAt }).ToListAsync(cancellationToken);

        var decidedPlans = plans.Count(plan => plan.Status is Domain.Enums.PlanStatus.Approved or Domain.Enums.PlanStatus.Rejected);
        var approvedPlans = plans.Count(plan => plan.Status == Domain.Enums.PlanStatus.Approved);

        var trends = Enumerable.Range(0, 6).Select(offset =>
        {
            var start = monthStart.AddMonths(offset - 5);
            var end = start.AddMonths(1);
            return new TrendPoint
            {
                Month = start.ToString("yyyy-MM"),
                Events = events.Count(item => item.CreatedAt >= start && item.CreatedAt < end),
                Plans = plans.Count(item => item.GeneratedAt >= start && item.GeneratedAt < end),
                Vendors = vendors.Count(item => item.CreatedAt >= start && item.CreatedAt < end)
            };
        }).ToList();

        return new AdminAnalyticsResponse
        {
            Users = new UserAnalytics
            {
                Total = users.Count,
                Active = users.Count(user => user.IsActive),
                EventPlanners = users.Count(user => user.UserRoles.Any(userRole => userRole.Role.RoleName == Domain.Enums.RoleName.EVENT_PLANNER)),
                Vendors = users.Count(user => user.UserRoles.Any(userRole => userRole.Role.RoleName == Domain.Enums.RoleName.VENDOR)),
                Administrators = users.Count(user => user.UserRoles.Any(userRole => userRole.Role.RoleName == Domain.Enums.RoleName.ADMIN)),
                RegistrationsThisMonth = users.Count(user => user.CreatedAt >= monthStart && user.CreatedAt < monthStart.AddMonths(1))
            },
            Events = new EventAnalytics
            {
                Total = events.Count,
                Active = events.Count(item => item.Status is Domain.Enums.EventStatus.PLANNING or Domain.Enums.EventStatus.CONFIRMED),
                Completed = events.Count(item => item.Status == Domain.Enums.EventStatus.COMPLETED),
                Cancelled = events.Count(item => item.Status == Domain.Enums.EventStatus.CANCELLED),
                CreatedThisMonth = events.Count(item => item.CreatedAt >= monthStart && item.CreatedAt < monthStart.AddMonths(1))
            },
            AiPlans = new AiPlanAnalytics
            {
                Generated = plans.Count,
                Approved = approvedPlans,
                Rejected = plans.Count(item => item.Status == Domain.Enums.PlanStatus.Rejected),
                Pending = plans.Count(item => item.Status == Domain.Enums.PlanStatus.PendingPlannerReview),
                ApprovalRate = decidedPlans == 0 ? 0 : decimal.Round(approvedPlans * 100m / decidedPlans, 2),
                GeneratedThisMonth = plans.Count(item => item.GeneratedAt >= monthStart && item.GeneratedAt < monthStart.AddMonths(1))
            },
            Vendors = new VendorAnalytics
            {
                Total = vendors.Count,
                Approved = vendors.Count(item => item.Status == Domain.Enums.VendorStatus.APPROVED),
                Pending = vendors.Count(item => item.Status == Domain.Enums.VendorStatus.PENDING)
            },
            Trends = trends
        };
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> ListUsersAsync(
        AdminUserQuery query, CancellationToken cancellationToken)
    {
        var users = db.Users.AsNoTracking().Include(user => user.UserRoles).ThenInclude(userRole => userRole.Role).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            users = users.Where(user => user.Email.ToLower().Contains(search) ||
                user.FirstName.ToLower().Contains(search) || user.LastName.ToLower().Contains(search));
        }
        if (query.Role.HasValue)
            users = users.Where(user => user.UserRoles.Any(userRole => userRole.Role.RoleName == query.Role.Value));
        var total = await users.CountAsync(cancellationToken);
        var items = await users.OrderByDescending(user => user.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<(IReadOnlyList<Vendor> Items, int TotalCount)> ListVendorsAsync(
        AdminVendorQuery query, CancellationToken cancellationToken)
    {
        var vendors = db.Vendors.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            vendors = vendors.Where(vendor => vendor.BusinessName.ToLower().Contains(search) ||
                vendor.ContactEmail.ToLower().Contains(search));
        }
        if (query.Status.HasValue) vendors = vendors.Where(vendor => vendor.Status == query.Status.Value);
        if (query.Category.HasValue) vendors = vendors.Where(vendor => vendor.Category == query.Category.Value);
        var total = await vendors.CountAsync(cancellationToken);
        var items = await vendors.OrderByDescending(vendor => vendor.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<(IReadOnlyList<EventPlanDraft> Items, int TotalCount)> ListPlansAsync(
        AdminPlanQuery query, CancellationToken cancellationToken)
    {
        var plans = db.EventPlanDrafts.AsNoTracking().Include(plan => plan.Event).AsQueryable();
        if (query.Status.HasValue) plans = plans.Where(plan => plan.Status == query.Status.Value);
        if (query.PlannerId.HasValue) plans = plans.Where(plan => plan.CreatedById == query.PlannerId.Value);
        if (query.EventType.HasValue) plans = plans.Where(plan => plan.Event.EventType == query.EventType.Value);
        if (query.DateFrom.HasValue) plans = plans.Where(plan => plan.GeneratedAt >= query.DateFrom.Value);
        if (query.DateTo.HasValue) plans = plans.Where(plan => plan.GeneratedAt < query.DateTo.Value.AddDays(1));
        if (query.ScoreMin.HasValue) plans = plans.Where(plan => plan.PlanCompletenessScore >= query.ScoreMin.Value);
        if (query.ScoreMax.HasValue) plans = plans.Where(plan => plan.PlanCompletenessScore <= query.ScoreMax.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            plans = plans.Where(plan => plan.Event.EventName.ToLower().Contains(search));
        }
        var descending = !string.Equals(query.SortOrder, "asc", StringComparison.OrdinalIgnoreCase);
        plans = query.SortBy?.ToLowerInvariant() switch
        {
            "status" => descending ? plans.OrderByDescending(plan => plan.Status) : plans.OrderBy(plan => plan.Status),
            "completeness" => descending ? plans.OrderByDescending(plan => plan.PlanCompletenessScore) : plans.OrderBy(plan => plan.PlanCompletenessScore),
            "eventname" => descending ? plans.OrderByDescending(plan => plan.Event.EventName) : plans.OrderBy(plan => plan.Event.EventName),
            _ => descending ? plans.OrderByDescending(plan => plan.GeneratedAt) : plans.OrderBy(plan => plan.GeneratedAt)
        };
        var total = await plans.CountAsync(cancellationToken);
        var items = await plans
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<EventPlanDraft?> GetPlanAsync(Guid id, CancellationToken cancellationToken) =>
        db.EventPlanDrafts.AsNoTracking().Include(plan => plan.Event).FirstOrDefaultAsync(plan => plan.Id == id, cancellationToken);
}
