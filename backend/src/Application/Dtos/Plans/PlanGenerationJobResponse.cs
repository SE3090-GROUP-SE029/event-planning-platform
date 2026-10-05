using System.Text.Json.Serialization;
using Domain.Entities;

namespace Application.Dtos.Plans;

public sealed record PlanGenerationJobResponse
{
    [JsonPropertyName("jobId")]
    public Guid JobId { get; init; }

    [JsonPropertyName("eventId")]
    public Guid EventId { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("planId")]
    public Guid? PlanId { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }

    public static PlanGenerationJobResponse From(PlanGenerationJob job) =>
        new()
        {
            JobId = job.Id,
            EventId = job.EventId,
            Status = job.Status.ToString(),
            PlanId = job.PlanId,
            Message = job.FailureMessage,
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt
        };
}
