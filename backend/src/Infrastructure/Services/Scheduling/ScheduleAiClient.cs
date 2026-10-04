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
        var eventDate = eventEntity.PreferredDate.Date;
        var eventDescription = string.IsNullOrWhiteSpace(eventEntity.Requirements)
            ? string.Empty
            : eventEntity.Requirements.Trim();

        var request = new
        {
            eventId = eventEntity.Id.ToString(),
            eventTitle = eventEntity.EventName,
            eventDescription,
            eventType = eventEntity.EventType.ToString(),
            eventDate = eventDate.ToString("yyyy-MM-dd"),
            eventStartTime = eventEntity.StartTime.ToString("HH:mm:ss"),
            eventEndTime = eventEntity.EndTime.ToString("HH:mm:ss"),
            vendorServiceContext = Array.Empty<object>(),
            title = eventEntity.EventName,
            event_type = eventEntity.EventType.ToString(),
            date = eventDate.ToString("yyyy-MM-dd"),
            start_time = eventEntity.StartTime.ToString("HH:mm:ss"),
            end_time = eventEntity.EndTime.ToString("HH:mm:ss"),
            guest_count = eventEntity.GuestCount,
            requirements = eventDescription
        };

        var path = string.IsNullOrWhiteSpace(options.Value.ScheduleGeneratePath)
            ? "/api/schedules/generate"
            : options.Value.ScheduleGeneratePath;

        using var response = await client.PostAsJsonAsync(path, request, cancellationToken);
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
