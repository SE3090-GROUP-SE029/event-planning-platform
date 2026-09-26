using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Application.Dtos.Plans;

/// <summary>Request to approve a plan currently awaiting planner review.</summary>
public record ApprovePlanRequest
{
    /// <summary>The plan to approve.</summary>
    [Required]
    [JsonPropertyName("planId")]
    public Guid PlanId { get; init; }

    /// <summary>Optional notes recorded with the approval.</summary>
    [MaxLength(500)]
    [JsonPropertyName("approverNotes")]
    public string? ApproverNotes { get; init; }

    /// <summary>Returns validation errors for this request.</summary>
    public IEnumerable<ValidationResult> GetValidationErrors()
    {
        if (PlanId == Guid.Empty)
            yield return new ValidationResult("PlanId is required.", [nameof(PlanId)]);
        if (ApproverNotes?.Length > 500)
            yield return new ValidationResult("Approver notes must be 500 characters or fewer.", [nameof(ApproverNotes)]);
    }
}

/// <summary>Severity assigned to a planner's rejection decision.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RejectionSeverity
{
    Minor = 0,
    Moderate = 1,
    Critical = 2
}

/// <summary>Request to reject a plan currently awaiting planner review.</summary>
public record RejectPlanRequest
{
    /// <summary>The plan to reject.</summary>
    [Required]
    [JsonPropertyName("planId")]
    public Guid PlanId { get; init; }

    /// <summary>Mandatory remarks explaining the rejection.</summary>
    [Required]
    [MinLength(20)]
    [MaxLength(1000)]
    [JsonPropertyName("remarks")]
    public string? Remarks { get; init; }

    /// <summary>The rejection severity.</summary>
    [JsonPropertyName("severity")]
    public RejectionSeverity Severity { get; init; }

    /// <summary>Returns validation errors for this request.</summary>
    public IEnumerable<ValidationResult> GetValidationErrors()
    {
        if (PlanId == Guid.Empty)
            yield return new ValidationResult("PlanId is required.", [nameof(PlanId)]);
        if (string.IsNullOrWhiteSpace(Remarks) || Remarks.Length is < 20 or > 1000)
            yield return new ValidationResult(
                "Remarks are required and must contain between 20 and 1,000 characters.",
                [nameof(Remarks)]);
        if (!Enum.IsDefined(Severity))
            yield return new ValidationResult("Severity is invalid.", [nameof(Severity)]);
    }
}
