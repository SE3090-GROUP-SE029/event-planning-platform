using Domain.Enums;

namespace Domain.Entities;

public class GuestAiReview
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long RegistrationSubmissionId { get; set; }
    public RegistrationSubmission RegistrationSubmission { get; set; } = null!;
    public AiAnalysisStatus Status { get; set; } = AiAnalysisStatus.PENDING;
    // Historical advisory value only. Active contracts and promotion use Decision.
    public AiRecommendation? Recommendation { get; set; }
    public AiDecision? Decision { get; set; }
    public double? Confidence { get; set; }
    public string[] Reasons { get; set; } = [];
    public string[] Flags { get; set; } = [];
    public string? Model { get; set; }
    public string? PromptVersion { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? AnalyzedAt { get; set; }
    public int Attempts { get; set; }
    public Guid? AttemptId { get; set; }
    public DateTimeOffset? LeaseExpiresAt { get; set; }
    public string? FailureCode { get; set; }
}
