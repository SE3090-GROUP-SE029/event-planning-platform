using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Domain.Enums;

namespace Application.Dtos.Plans;

/// <summary>Describes a risk identified while generating an event plan.</summary>
public record RiskDto
{
    /// <summary>A concise description of the risk, limited to 500 characters.</summary>
    [JsonPropertyName("risk")]
    public string? Risk { get; init; }

    /// <summary>The risk severity: Low, Medium, or High.</summary>
    [JsonPropertyName("severity")]
    public string? Severity { get; init; }

    /// <summary>An actionable recommendation, limited to 1,000 characters.</summary>
    [JsonPropertyName("recommendation")]
    public string? Recommendation { get; init; }

    /// <summary>Returns validation errors for this risk.</summary>
    public IEnumerable<ValidationResult> GetValidationErrors()
    {
        if (string.IsNullOrWhiteSpace(Risk) || Risk.Length > 500)
            yield return new ValidationResult("Risk is required and must be 500 characters or fewer.", [nameof(Risk)]);

        if (!Enum.TryParse<RiskSeverity>(Severity, ignoreCase: true, out _))
            yield return new ValidationResult("Severity must be Low, Medium, or High.", [nameof(Severity)]);

        if (string.IsNullOrWhiteSpace(Recommendation) || Recommendation.Length > 1000)
            yield return new ValidationResult(
                "Recommendation is required and must be 1,000 characters or fewer.",
                [nameof(Recommendation)]);
    }

    /// <inheritdoc />
    public override string ToString() => $"{Severity}: {Risk} ({Recommendation})";
}
