using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>A versioned plan generated for an event and reviewed by a planner.</summary>
public class EventPlanDraft : IValidatableObject
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public int Version { get; set; }
    public PlanStatus Status { get; set; } = PlanStatus.Draft;

    [Required, MinLength(1)]
    public List<string> ServiceCategories { get; set; } = [];
    public Dictionary<string, decimal> BudgetAllocation { get; set; } = [];
    public List<string> TargetVendorTypes { get; set; } = [];
    public Dictionary<string, string> ProposedTimeline { get; set; } = [];
    public string Rationale { get; set; } = string.Empty;
    [Range(0, 100)]
    public int PlanCompletenessScore { get; set; }
    public string ValidationSummary { get; set; } = string.Empty;

    public List<IdentifiedRisk> IdentifiedRisks { get; set; } = [];
    public List<MissingRequirement> MissingRequirements { get; set; } = [];
    [Required]
    public EventSnapshot EventSnapshot { get; set; } = null!;

    public DateTime? PlannerDecisionAt { get; set; }
    public string? PlannerRemarks { get; set; }
    public Guid? ApprovedById { get; set; }
    public Guid? RejectedById { get; set; }

    public DateTime GeneratedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedById { get; set; }

    public Event Event { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public User? ApprovedBy { get; set; }
    public User? RejectedBy { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Version <= 0)
            yield return new ValidationResult("Version must be greater than zero.", [nameof(Version)]);
        if (ServiceCategories.Count == 0 || ServiceCategories.Any(string.IsNullOrWhiteSpace))
            yield return new ValidationResult("At least one service category is required.", [nameof(ServiceCategories)]);
        if (BudgetAllocation.Any(item => item.Value < 0))
            yield return new ValidationResult("Budget allocations cannot be negative.", [nameof(BudgetAllocation)]);
        if (EventSnapshot is null)
            yield return new ValidationResult("EventSnapshot is required.", [nameof(EventSnapshot)]);
        else if (BudgetAllocation.Values.Sum() > EventSnapshot.Budget)
            yield return new ValidationResult(
                "Budget allocation cannot exceed the event budget.",
                [nameof(BudgetAllocation)]);
        if (MissingRequirements
            .GroupBy(item => (item.Requirement, item.Category), StringTupleComparer.Instance)
            .Any(group => group.Count() > 1))
            yield return new ValidationResult(
                "Missing requirements cannot contain duplicates.",
                [nameof(MissingRequirements)]);
    }

    private sealed class StringTupleComparer : IEqualityComparer<(string Requirement, RequirementCategory Category)>
    {
        public static StringTupleComparer Instance { get; } = new();

        public bool Equals(
            (string Requirement, RequirementCategory Category) x,
            (string Requirement, RequirementCategory Category) y) =>
            string.Equals(x.Requirement, y.Requirement, StringComparison.OrdinalIgnoreCase) &&
            x.Category == y.Category;

        public int GetHashCode((string Requirement, RequirementCategory Category) value) =>
            HashCode.Combine(value.Requirement.ToUpperInvariant(), value.Category);
    }
}
