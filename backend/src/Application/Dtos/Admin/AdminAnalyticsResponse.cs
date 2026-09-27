namespace Application.Dtos.Admin;

public sealed class AdminAnalyticsResponse
{
    public UserAnalytics Users { get; init; } = new();
    public EventAnalytics Events { get; init; } = new();
    public AiPlanAnalytics AiPlans { get; init; } = new();
    public VendorAnalytics Vendors { get; init; } = new();
    public IReadOnlyList<TrendPoint> Trends { get; init; } = [];
}

public sealed class UserAnalytics
{
    public int Total { get; init; }
    public int Active { get; init; }
    public int EventPlanners { get; init; }
    public int Vendors { get; init; }
    public int Administrators { get; init; }
    public int RegistrationsThisMonth { get; init; }
}

public sealed class EventAnalytics
{
    public int Total { get; init; }
    public int Active { get; init; }
    public int Completed { get; init; }
    public int Cancelled { get; init; }
    public int CreatedThisMonth { get; init; }
}

public sealed class AiPlanAnalytics
{
    public int Generated { get; init; }
    public int Approved { get; init; }
    public int Rejected { get; init; }
    public int Pending { get; init; }
    /// <summary>Approved / (Approved + Rejected); undecided plans are excluded.</summary>
    public decimal ApprovalRate { get; init; }
    public int GeneratedThisMonth { get; init; }
}

public sealed class VendorAnalytics
{
    public int Total { get; init; }
    public int Approved { get; init; }
    public int Pending { get; init; }
}

public sealed class TrendPoint
{
    public string Month { get; init; } = string.Empty;
    public int Events { get; init; }
    public int Plans { get; init; }
    public int Vendors { get; init; }
}
