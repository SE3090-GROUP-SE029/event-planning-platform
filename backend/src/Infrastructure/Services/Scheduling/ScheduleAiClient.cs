using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
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

        var stopwatch = Stopwatch.StartNew();
        var requestStartedAt = DateTimeOffset.UtcNow;
        int? statusCode = null;
        string? timeoutSource = null;
        string? cancellationSource = null;
        string? exceptionType = null;
        logger.LogInformation(
            "AI schedule request started for event {EventId} at {RequestStartedAt}, path {Path}, timeout {TimeoutMilliseconds} ms",
            eventEntity.Id,
            requestStartedAt,
            path,
            client.Timeout.TotalMilliseconds);

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(request)
            };
            using var response = await client.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            statusCode = (int)response.StatusCode;
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            logger.LogInformation(
                "AI schedule response received for event {EventId}: status {StatusCode}, elapsed {ElapsedMilliseconds} ms",
                eventEntity.Id,
                statusCode,
                stopwatch.ElapsedMilliseconds);

            if (response.StatusCode is System.Net.HttpStatusCode.GatewayTimeout
                or System.Net.HttpStatusCode.RequestTimeout)
            {
                timeoutSource = "AI Service";
                throw new TimeoutException(
                    $"AI schedule service returned HTTP {statusCode.Value}.");
            }

            response.EnsureSuccessStatusCode();
            var output = JsonSerializer.Deserialize<AiScheduleApiResponse>(
                responseBody,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (output?.Activities is null || output.Conflicts is null)
            {
                throw new InvalidOperationException(
                    "The AI schedule service response must contain activities and conflicts arrays.");
            }

            logger.LogInformation(
                "Deserialized AI schedule for event {EventId}: {ActivityCount} activities and {ConflictCount} conflicts",
                eventEntity.Id,
                output.Activities.Count,
                output.Conflicts.Count);

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
        catch (OperationCanceledException ex)
            when (!cancellationToken.IsCancellationRequested)
        {
            timeoutSource = "Backend";
            exceptionType = ex.GetType().FullName;
            logger.LogError(
                ex,
                "AI schedule request timed out for event {EventId} after {ElapsedMilliseconds} ms; timeout source {TimeoutSource}, status {StatusCode}, exception type {ExceptionType}",
                eventEntity.Id,
                stopwatch.ElapsedMilliseconds,
                timeoutSource,
                statusCode?.ToString() ?? "none",
                exceptionType);
            throw new TimeoutException(
                $"AI schedule request exceeded {client.Timeout.TotalMilliseconds:0} ms.",
                ex);
        }
        catch (OperationCanceledException ex)
        {
            cancellationSource = "ScheduleGenerationToken";
            exceptionType = ex.GetType().FullName;
            logger.LogWarning(
                ex,
                "AI schedule request cancelled for event {EventId}; cancellation source {CancellationSource}, status {StatusCode}, exception type {ExceptionType}",
                eventEntity.Id,
                cancellationSource,
                statusCode?.ToString() ?? "none",
                exceptionType);
            throw;
        }
        catch (Exception ex)
        {
            exceptionType = ex.GetType().FullName;
            logger.LogError(
                ex,
                "AI schedule request failed for event {EventId}; status {StatusCode}, exception type {ExceptionType}",
                eventEntity.Id,
                statusCode?.ToString() ?? "none",
                exceptionType);
            throw;
        }
        finally
        {
            logger.LogInformation(
                "AI schedule request completed for event {EventId} at {RequestCompletedAt}; total duration {ElapsedMilliseconds} ms; status {StatusCode}; timeout source {TimeoutSource}; cancellation source {CancellationSource}; exception type {ExceptionType}",
                eventEntity.Id,
                DateTimeOffset.UtcNow,
                stopwatch.ElapsedMilliseconds,
                statusCode?.ToString() ?? "not received",
                timeoutSource ?? "None",
                cancellationSource ?? "None",
                exceptionType ?? "None");
        }
    }

    private sealed class AiScheduleApiResponse
    {
        [JsonPropertyName("activities")]
        public List<AiScheduleApiActivity>? Activities { get; init; }

        [JsonPropertyName("conflicts")]
        public List<string>? Conflicts { get; init; }
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
