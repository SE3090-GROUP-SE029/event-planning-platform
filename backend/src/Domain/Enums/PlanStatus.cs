using System.ComponentModel;
using System.Runtime.Serialization;

namespace Domain.Enums;

/// <summary>Lifecycle states for an event plan draft.</summary>
public enum PlanStatus
{
    [Description("Draft")]
    [EnumMember(Value = "draft")]
    Draft = 0,

    [Description("Pending Planner Review")]
    [EnumMember(Value = "pending_planner_review")]
    PendingPlannerReview = 1,

    [Description("Approved")]
    [EnumMember(Value = "approved")]
    Approved = 2,

    [Description("Rejected")]
    [EnumMember(Value = "rejected")]
    Rejected = 3,

    [Description("Superseded")]
    [EnumMember(Value = "superseded")]
    Superseded = 4
}

public static class PlanStatusRules
{
    public static bool IsValidTransition(PlanStatus from, PlanStatus to) =>
        from != to &&
        (to == PlanStatus.Superseded ||
         (from == PlanStatus.Draft && to == PlanStatus.PendingPlannerReview) ||
         (from == PlanStatus.PendingPlannerReview &&
          to is PlanStatus.Approved or PlanStatus.Rejected));

    public static bool CanBeApproved(PlanStatus status) =>
        status == PlanStatus.PendingPlannerReview;

    public static bool CanBeRejected(PlanStatus status) =>
        status == PlanStatus.PendingPlannerReview;

    public static string GetDisplayName(PlanStatus status) =>
        status switch
        {
            PlanStatus.Draft => "Draft",
            PlanStatus.PendingPlannerReview => "Pending Planner Review",
            PlanStatus.Approved => "Approved",
            PlanStatus.Rejected => "Rejected",
            PlanStatus.Superseded => "Superseded",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown plan status.")
        };
}
