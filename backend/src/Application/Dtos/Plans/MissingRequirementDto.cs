using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Application.Dtos.Plans;

/// <summary>Describes information required to complete an event plan.</summary>
public record MissingRequirementDto
{
    /// <summary>The missing requirement name, limited to 200 characters.</summary>
    [JsonPropertyName("requirement")]
    public string? Requirement { get; init; }

    /// <summary>Why the requirement is needed, limited to 500 characters.</summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    /// <summary>Returns validation errors for this missing requirement.</summary>
    public IEnumerable<ValidationResult> GetValidationErrors()
    {
        if (string.IsNullOrWhiteSpace(Requirement) || Requirement.Length > 200)
            yield return new ValidationResult(
                "Requirement is required and must be 200 characters or fewer.",
                [nameof(Requirement)]);

        if (string.IsNullOrWhiteSpace(Reason) || Reason.Length > 500)
            yield return new ValidationResult(
                "Reason is required and must be 500 characters or fewer.",
                [nameof(Reason)]);
    }

    /// <inheritdoc />
    public override string ToString() => $"{Requirement}: {Reason}";
}
