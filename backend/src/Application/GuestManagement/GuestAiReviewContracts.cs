using Domain.Entities;
using Domain.Enums;

namespace Application.GuestManagement;

// Explicit projections: never serialize registration entities to the AI service.
public record AiGuestContext(string FullName, string EmailAddress, string? Organisation, string? PhoneNumber);
public record AiEventContext(string EventName, string? RequirementNotes);
public record AiComparisonContext(AiGuestContext Guest, DateTimeOffset RegisteredAt);
public record GuestAiContext(AiGuestContext Guest, AiEventContext Event, DateTimeOffset RegisteredAt,
    IReadOnlyList<AiComparisonContext> Comparisons, bool ComparisonsLimited,
    IReadOnlyList<AiQuestionAnswer>? Questions = null);
public record GuestAiDecision(AiDecision Decision, double Confidence, string[] Reasons,
    string[] Flags, string Model, string PromptVersion)
{
    public void Validate()
    {
        if (!Enum.IsDefined(Decision) || !double.IsFinite(Confidence) || Confidence is < 0 or > 1 ||
            Reasons is null || Reasons.Length is < 1 or > 10 || Reasons.Any(r => !ValidText(r, 500)) ||
            Flags is null || Flags.Length > 10 || Flags.Any(f => !ValidText(f, 80)) ||
            !ValidText(Model, 120) || !ValidText(PromptVersion, 40))
            throw new AiAnalysisException("invalid_ai_response");
    }

    private static bool ValidText(string? value, int maximum)
        => !string.IsNullOrWhiteSpace(value) && value.Length <= maximum && !value.Any(char.IsControl);
}

public record GuestAiClaim(Guid ReviewId, long RegistrationSubmissionId, Guid AttemptId);

public interface IGuestAiClient
{
    Task<GuestAiDecision> AnalyzeAsync(GuestAiContext context, CancellationToken ct);
}

public interface IGuestAiReviewRepository
{
    Task<GuestAiReview?> FindAsync(long registrationId, CancellationToken ct);
    Task<GuestAiReview> QueueAsync(Guid eventId, long registrationId, DateTimeOffset now, CancellationToken ct);
    Task<GuestAiClaim?> ClaimAsync(DateTimeOffset now, DateTimeOffset leaseExpiresAt, CancellationToken ct);
    Task<GuestAiContext> ContextAsync(long registrationId, CancellationToken ct);
    Task<bool> CompleteAsync(GuestAiClaim claim, GuestAiDecision decision, DateTimeOffset now, CancellationToken ct);
    Task<bool> FailAsync(GuestAiClaim claim, string code, CancellationToken ct);
}

// Only stable, non-sensitive codes cross the service boundary or enter logs.
public class AiAnalysisException(string code) : Exception(code)
{
    public string Code { get; } = code;
}
