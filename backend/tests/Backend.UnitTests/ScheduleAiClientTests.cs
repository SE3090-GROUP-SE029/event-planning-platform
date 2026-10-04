using System.Net;
using System.Text;
using System.Text.Json;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Services.Planning;
using Infrastructure.Services.Scheduling;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Backend.UnitTests;

public sealed class ScheduleAiClientTests
{
    [Fact]
    public async Task GenerateScheduleAsync_UsesScheduleGeneratePath()
    {
        const string responseJson = """
            {
              "activities": [
                {
                  "title": "Welcome",
                  "description": "Guest arrival",
                  "start_time": "2026-10-15T09:00:00Z",
                  "end_time": "2026-10-15T09:30:00Z"
                }
              ],
              "conflicts": []
            }
            """;
        var handler = new CapturingHttpMessageHandler(responseJson);
        var client = new ScheduleAiClient(
            new TestHttpClientFactory(new HttpClient(handler)
            {
                BaseAddress = new Uri("http://agentic-ai.test")
            }),
            Options.Create(new AgenticAiOptions
            {
                GeneratePath = "/api/coordinator/generate",
                ScheduleGeneratePath = "/api/schedules/generate"
            }),
            NullLogger<ScheduleAiClient>.Instance);

        var result = await client.GenerateScheduleAsync(new Event
        {
            Id = Guid.NewGuid(),
            EventName = "Conference launch",
            EventType = EventType.CORPORATE,
            PreferredDate = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0),
            EventDuration = TimeSpan.FromHours(2),
            GuestCount = 100,
            Requirements = "Keep sessions concise."
        });

        Assert.Equal("/api/schedules/generate", handler.RequestPath);
        using var json = JsonDocument.Parse(handler.RequestBody!);
        var root = json.RootElement;
        Assert.Equal("Conference launch", root.GetProperty("eventTitle").GetString());
        Assert.Equal("CORPORATE", root.GetProperty("eventType").GetString());
        Assert.Equal("2026-10-15", root.GetProperty("eventDate").GetString());
        Assert.Equal("09:00:00", root.GetProperty("eventStartTime").GetString());
        Assert.Equal("11:00:00", root.GetProperty("eventEndTime").GetString());
        Assert.Equal("Keep sessions concise.", root.GetProperty("eventDescription").GetString());
        Assert.Equal(JsonValueKind.Array, root.GetProperty("vendorServiceContext").ValueKind);
        Assert.Equal("Welcome", Assert.Single(result.Activities).Title);
    }

    private sealed class TestHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class CapturingHttpMessageHandler(
        string responseBody,
        HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        public string? RequestPath { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestPath = request.RequestUri?.AbsolutePath;
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
