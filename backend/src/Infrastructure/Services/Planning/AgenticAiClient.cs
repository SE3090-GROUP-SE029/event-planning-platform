using System.Net;
using System.Net.Http.Json;
using Application.Dtos.Plans;
using Application.Services.Planning;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Planning;

public sealed class AgenticAiClient(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<AgenticAiClient> logger) : IAgenticAiClient
{
    public async Task<CoordinatorPlanResponse> GeneratePlanAsync(
        Event eventEntity,
        CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("AgenticAI");
        var request = new
        {
            eventId = eventEntity.Id,
            @event = new
            {
                name = eventEntity.PreferredVenue ?? "Event",
                type = eventEntity.EventType.ToString(),
                date = eventEntity.PreferredDate,
                location = eventEntity.PreferredVenue,
                guest_count = eventEntity.GuestCount,
                budget = eventEntity.Budget,
                requirements = eventEntity.Requirements
            }
        };

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using var response = await client.PostAsJsonAsync(
                    configuration["AgenticAI:GeneratePath"] ?? "/api/coordinator/generate",
                    request,
                    cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<CoordinatorPlanResponse>(
                        cancellationToken)
                        ?? throw new InvalidOperationException("Agentic AI returned an empty response.");
                }

                if (response.StatusCode is not (HttpStatusCode.RequestTimeout or
                    HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable))
                    response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException) when (attempt < 3)
            {
                logger.LogWarning("Agentic AI request failed on attempt {Attempt}", attempt);
            }

            if (attempt < 3)
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)), cancellationToken);
        }

        throw new HttpRequestException("Agentic AI service was unavailable after three attempts.");
    }
}
