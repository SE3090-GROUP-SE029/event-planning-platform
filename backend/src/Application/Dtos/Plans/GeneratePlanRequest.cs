using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Application.Dtos.Plans;

/// <summary>Request to generate or regenerate an event plan.</summary>
public record GeneratePlanRequest
{
    /// <summary>The event for which a plan should be generated.</summary>
    [Required]
    [JsonPropertyName("eventId")]
    public Guid EventId { get; init; }

    /// <summary>Whether this request replaces an existing plan version.</summary>
    [JsonPropertyName("regenerate")]
    public bool Regenerate { get; init; }

    /// <summary>Why the plan is being regenerated, when <see cref="Regenerate"/> is true.</summary>
    [MaxLength(500)]
    [JsonPropertyName("regenerationReason")]
    public string? RegenerationReason { get; init; }

    /// <summary>Returns validation errors for this request.</summary>
    public IEnumerable<ValidationResult> GetValidationErrors()
    {
        if (EventId == Guid.Empty)
            yield return new ValidationResult("EventId is required.", [nameof(EventId)]);

        if (RegenerationReason?.Length > 500)
            yield return new ValidationResult(
                "Regeneration reason must be 500 characters or fewer.",
                [nameof(RegenerationReason)]);

        if (Regenerate && string.IsNullOrWhiteSpace(RegenerationReason))
            yield return new ValidationResult(
                "Regeneration reason is required when regenerating a plan.",
                [nameof(RegenerationReason)]);
    }
}
