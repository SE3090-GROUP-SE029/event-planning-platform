using Domain.Enums;
using Domain.Entities;

namespace Application.Dtos.Admin;

public sealed class AdminUserQuery
{
    public string? Search { get; set; }
    public RoleName? Role { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public sealed class AdminUserListResponse
{
    public IReadOnlyList<AdminUserResponse> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}

public sealed class AdminUserResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
}

public sealed class AdminVendorQuery
{
    public string? Search { get; set; }
    public VendorStatus? Status { get; set; }
    public BusinessCategory? Category { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public sealed class AdminVendorListResponse
{
    public IReadOnlyList<AdminVendorResponse> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}

public sealed class AdminVendorResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string BusinessName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ProfileImageUrl { get; init; }
    public string? WebsiteUrl { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class AdminPlanQuery
{
    public PlanStatus? Status { get; set; }
    public Guid? PlannerId { get; set; }
    public EventType? EventType { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? ScoreMin { get; set; }
    public int? ScoreMax { get; set; }
    public string? Search { get; set; }
    public string? SortBy { get; set; } = "generatedAt";
    public string? SortOrder { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public sealed class AdminPlanListResponse
{
    public IReadOnlyList<AdminPlanResponse> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}

public sealed class AdminPlanResponse
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public string EventName { get; init; } = string.Empty;
    public int Version { get; init; }
    public PlanStatus Status { get; init; }
    public int PlanCompletenessScore { get; init; }
    public DateTime GeneratedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? PlannerDecisionAt { get; init; }
    public Guid CreatedById { get; init; }
    public Guid? ApprovedById { get; init; }
    public Guid? RejectedById { get; init; }
    public string Rationale { get; init; } = string.Empty;
    public List<string> ServiceCategories { get; init; } = [];
    public Dictionary<string, decimal> BudgetAllocation { get; init; } = [];
    public Dictionary<string, string> ProposedTimeline { get; init; } = [];
    public List<string> TargetVendorTypes { get; init; } = [];
    public List<IdentifiedRisk> IdentifiedRisks { get; init; } = [];
    public List<MissingRequirement> MissingRequirements { get; init; } = [];
    public EventSnapshot EventSnapshot { get; init; } = null!;
    public string ValidationSummary { get; init; } = string.Empty;
    public string? PlannerRemarks { get; init; }
}

public sealed class AdminHealthResponse
{
    public string Api { get; init; } = "healthy";
    public string Database { get; init; } = "unknown";
    public DateTime CheckedAt { get; init; }
}
