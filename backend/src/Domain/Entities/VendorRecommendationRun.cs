namespace Domain.Entities;

/// <summary>One persisted AI vendor recommendation run for an approved plan.</summary>
public class VendorRecommendationRun
{
    public const string PendingStatus = "Pending";
    public const string RunningStatus = "Running";
    public const string CompletedStatus = "Completed";
    public const string FailedStatus = "Failed";

    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid EventPlanDraftId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public int CandidateCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = CompletedStatus;
    public string Stage { get; set; } = string.Empty;
    public string? FailureMessage { get; set; }
    public string? SourceNote { get; set; }

    public Event Event { get; set; } = null!;
    public EventPlanDraft EventPlanDraft { get; set; } = null!;
    public ICollection<VendorRecommendationItem> Items { get; set; } = [];
}
