using Domain.Enums;

namespace Domain.Entities;

public sealed class PlanGenerationJob
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid RequestedById { get; set; }
    public Guid? PlanId { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public PlanGenerationJobStatus Status { get; set; } = PlanGenerationJobStatus.Queued;
    public bool Regenerate { get; set; }
    public int AttemptCount { get; set; }
    public string? FailureMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Event Event { get; set; } = null!;
    public User RequestedBy { get; set; } = null!;
}
