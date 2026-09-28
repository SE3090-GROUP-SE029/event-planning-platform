using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Application.Common.Interfaces;
using Application.Services.Scheduling;
using Domain.Entities;
using Infrastructure.Services.Planning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Scheduling;

public sealed class ScheduleAiClient(
    IHttpClientFactory httpClientFactory,
    IOptions<AgenticAiOptions> options,
    ILogger<ScheduleAiClient> logger) : IScheduleAiClient
{
    public async Task<AiScheduleGenerationResult> GenerateScheduleAsync(
        Event eventEntity,
        CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("AgenticAI");
        var eventStart = eventEntity.PreferredDate.ToUniversalTime();
        var eventEnd = eventStart.Add(eventEntity.EventDuration).ToUniversalTime();

        var request = new
        {
            event_id = eventEntity.Id.ToString(),
            title = eventEntity.EventName,
            event_type = eventEntity.EventType.ToString(),
            date = eventStart.Date.ToString("yyyy-MM-dd"),
            start_time = eventStart.ToString("O"),
            end_time = eventEnd.ToString("O"),
            guest_count = eventEntity.GuestCount,
            requirements = string.IsNullOrWhiteSpace(eventEntity.Requirements) ? string.Empty : eventEntity.Requirements
        };

        using var response = await client.PostAsJsonAsync(options.Value.GeneratePath, request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning(
                "AI schedule generation failed for event {EventId} with status {StatusCode}: {Payload}",
                eventEntity.Id,
                (int)response.StatusCode,
                payload);
            response.EnsureSuccessStatusCode();
        }

        var output = await response.Content.ReadFromJsonAsync<AiScheduleApiResponse>(cancellationToken)
            ?? throw new InvalidOperationException("The AI schedule service returned an empty response.");

        return new AiScheduleGenerationResult
        {
            Activities = output.Activities.Select(a => new AiScheduleActivity
            {
                Title = a.Title,
                Description = a.Description,
                StartTime = a.StartTime,
                EndTime = a.EndTime
            }).ToList(),
            Conflicts = output.Conflicts
        };
    }

    private sealed class AiScheduleApiResponse
    {
        [JsonPropertyName("activities")]
        public List<AiScheduleApiActivity> Activities { get; init; } = [];

        [JsonPropertyName("conflicts")]
        public List<string> Conflicts { get; init; } = [];
    }

    private sealed class AiScheduleApiActivity
    {
        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("start_time")]
        public string StartTime { get; init; } = string.Empty;

        [JsonPropertyName("end_time")]
        public string EndTime { get; init; } = string.Empty;
    }
}
