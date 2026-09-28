using Application.Common.Interfaces;
using Application.Dtos.Admin;
using Domain.Entities;

namespace Application.Services.Admin;

public sealed class AdminReadService(IAdminReadRepository repository) : IAdminReadService
{
    public Task<AdminAnalyticsResponse> GetAnalyticsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return repository.GetAnalyticsAsync(new DateTime(now.Year, now.Month, 1), cancellationToken);
    }

    public async Task<AdminUserListResponse> ListUsersAsync(AdminUserQuery query, CancellationToken cancellationToken)
    {
        ValidatePaging(query.Page, query.PageSize);
        var (items, total) = await repository.ListUsersAsync(query, cancellationToken);
        return new AdminUserListResponse
        {
            Items = items.Select(MapUser).ToList(), Page = query.Page, PageSize = query.PageSize,
            TotalCount = total, TotalPages = Pages(total, query.PageSize)
        };
    }

    public async Task<AdminVendorListResponse> ListVendorsAsync(AdminVendorQuery query, CancellationToken cancellationToken)
    {
        ValidatePaging(query.Page, query.PageSize);
        var (items, total) = await repository.ListVendorsAsync(query, cancellationToken);
        return new AdminVendorListResponse
        {
            Items = items.Select(MapVendor).ToList(), Page = query.Page, PageSize = query.PageSize,
            TotalCount = total, TotalPages = Pages(total, query.PageSize)
        };
    }

    public async Task<AdminPlanListResponse> ListPlansAsync(AdminPlanQuery query, CancellationToken cancellationToken)
    {
        ValidatePaging(query.Page, query.PageSize);
        var (items, total) = await repository.ListPlansAsync(query, cancellationToken);
        return new AdminPlanListResponse
        {
            Items = items.Select(MapPlan).ToList(), Page = query.Page, PageSize = query.PageSize,
            TotalCount = total, TotalPages = Pages(total, query.PageSize)
        };
    }

    public async Task<AdminPlanResponse> GetPlanAsync(Guid id, CancellationToken cancellationToken) =>
        MapPlan(await repository.GetPlanAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Plan not found."));

    private static AdminUserResponse MapUser(User user) => new()
    {
        Id = user.Id, Email = user.Email, FirstName = user.FirstName, LastName = user.LastName,
        CreatedAt = user.CreatedAt, IsActive = user.IsActive,
        Roles = user.UserRoles.Select(userRole => userRole.Role.RoleName.ToString()).Distinct().ToList()
    };

    private static AdminVendorResponse MapVendor(Vendor vendor) => new()
    {
        Id = vendor.Id, UserId = vendor.UserId, BusinessName = vendor.BusinessName,
        Category = vendor.Category.ToString(), ContactEmail = vendor.ContactEmail, ContactPhone = vendor.ContactPhone,
        Address = vendor.Address, Description = vendor.Description, ProfileImageUrl = vendor.ProfileImageUrl,
        WebsiteUrl = vendor.WebsiteUrl, Status = vendor.Status.ToString(), CreatedAt = vendor.CreatedAt, UpdatedAt = vendor.UpdatedAt
    };

    private static AdminPlanResponse MapPlan(EventPlanDraft plan) => new()
    {
        Id = plan.Id, EventId = plan.EventId, EventName = plan.Event?.EventName ?? plan.EventSnapshot.EventName,
        Version = plan.Version, Status = plan.Status, PlanCompletenessScore = plan.PlanCompletenessScore,
        GeneratedAt = plan.GeneratedAt, CreatedAt = plan.CreatedAt, UpdatedAt = plan.UpdatedAt,
        PlannerDecisionAt = plan.PlannerDecisionAt, CreatedById = plan.CreatedById,
        ApprovedById = plan.ApprovedById, RejectedById = plan.RejectedById, Rationale = plan.Rationale,
        ServiceCategories = plan.ServiceCategories, BudgetAllocation = plan.BudgetAllocation,
        ProposedTimeline = plan.ProposedTimeline, TargetVendorTypes = plan.TargetVendorTypes,
        IdentifiedRisks = plan.IdentifiedRisks, MissingRequirements = plan.MissingRequirements,
        EventSnapshot = plan.EventSnapshot, ValidationSummary = plan.ValidationSummary, PlannerRemarks = plan.PlannerRemarks
    };

    private static void ValidatePaging(int page, int pageSize)
    {
        if (page < 1) throw new ArgumentException("Page must be greater than zero.");
        if (pageSize is < 1 or > 100) throw new ArgumentException("PageSize must be between 1 and 100.");
    }

    private static int Pages(int total, int pageSize) => total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
}
