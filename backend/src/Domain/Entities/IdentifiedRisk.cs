using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>A value object describing a risk found during plan generation.</summary>
public sealed class IdentifiedRisk : IEquatable<IdentifiedRisk>
{
    private IdentifiedRisk() { }

    public IdentifiedRisk(string risk, RiskSeverity severity, string recommendation, DateTime? detectedAt = null)
    {
        Risk = ValidateText(risk, 500, nameof(risk));
        Recommendation = ValidateText(recommendation, 1000, nameof(recommendation));
        if (!Enum.IsDefined(severity))
            throw new ArgumentOutOfRangeException(nameof(severity));

        Severity = severity;
        DetectedAt = detectedAt ?? DateTime.UtcNow;
    }

    [Required, MaxLength(500)]
    public string Risk { get; init; } = string.Empty;

    public RiskSeverity Severity { get; init; }

    [Required, MaxLength(1000)]
    public string Recommendation { get; init; } = string.Empty;

    public DateTime DetectedAt { get; init; }

    public bool Equals(IdentifiedRisk? other) =>
        other is not null &&
        Risk == other.Risk &&
        Severity == other.Severity &&
        Recommendation == other.Recommendation &&
        DetectedAt == other.DetectedAt;

    public override bool Equals(object? obj) => Equals(obj as IdentifiedRisk);

    public override int GetHashCode() => HashCode.Combine(Risk, Severity, Recommendation, DetectedAt);

    public static bool operator ==(IdentifiedRisk? left, IdentifiedRisk? right) =>
        EqualityComparer<IdentifiedRisk>.Default.Equals(left, right);

    public static bool operator !=(IdentifiedRisk? left, IdentifiedRisk? right) => !(left == right);

    private static string ValidateText(string value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength)
            throw new ArgumentException($"Must contain between 1 and {maxLength} characters.", parameterName);
        return value.Trim();
    }
}
