using System.Net;
using System.Text;
using Application.Dtos.Plans;
using Domain.Entities;
using Infrastructure.Services.Planning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.UnitTests;

public sealed class AgenticAiClientTests
{
    [Fact]
    public async Task GeneratePlanAsync_MapsAgenticAiSnakeCaseAndStructuredCollections()
    {
        const string responseJson = """
            {
              "service_categories": ["Venue", "Catering"],
              "budget_allocation": [
                { "category": "Venue", "amount": 600, "percentage_of_total": 60 },
                { "category": "Catering", "amount": 400, "percentage_of_total": 40 }
              ],
              "target_vendor_types": ["Venue provider"],
              "proposed_timeline": [
                { "phase_name": "Planning", "timing": "12 weeks before", "description": "Set priorities" },
                { "phase_name": "Booking", "timing": "8 weeks before", "description": "Book services" },
                { "phase_name": "Confirmation", "timing": "1 week before", "description": "Confirm details" }
              ],
              "rationale": "A detailed event plan based on the owner requirements and guest count.",
              "identified_risks": [
                { "risk": "Late booking", "severity": "Medium", "recommendation": "Reserve early" }
              ],
              "missing_requirements": [
                { "requirement": "Dietary needs", "reason": "Catering needs final guest preferences" }
              ],
              "plan_completeness_score": 85,
              "validation_summary": "Coordinator self-validation passed."
            }
            """;
        var handler = new StubHttpMessageHandler(responseJson);
        var client = new AgenticAiClient(
            new TestHttpClientFactory(new HttpClient(handler)
            {
                BaseAddress = new Uri("http://agentic-ai.test")
            }),
            new ConfigurationBuilder().Build(),
            NullLogger<AgenticAiClient>.Instance);

        var result = await client.GeneratePlanAsync(new Event
        {
            Id = Guid.NewGuid(),
            Budget = 1000m
        });

        Assert.Equal(["Venue", "Catering"], result.ServiceCategories);
        Assert.Equal(600m, result.BudgetAllocation["Venue"]);
        Assert.Equal(400m, result.BudgetAllocation["Catering"]);
        Assert.Equal("12 weeks before: Set priorities", result.ProposedTimeline["Planning"]);
        Assert.Equal("Venue provider", Assert.Single(result.TargetVendorTypes));
        Assert.Equal("Late booking", Assert.Single(result.IdentifiedRisks).Risk);
        Assert.Equal("Dietary needs", Assert.Single(result.MissingRequirements).Requirement);
        Assert.Equal(85, result.PlanCompletenessScore);
    }

    [Fact]
    public async Task GeneratePlanAsync_DoesNotRetryAnUnsuccessfulAgentRequest()
    {
        var handler = new StubHttpMessageHandler(
            """{"detail":"provider unavailable"}""",
            HttpStatusCode.BadGateway);
        var client = new AgenticAiClient(
            new TestHttpClientFactory(new HttpClient(handler)
            {
                BaseAddress = new Uri("http://agentic-ai.test")
            }),
            new ConfigurationBuilder().Build(),
            NullLogger<AgenticAiClient>.Instance);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GeneratePlanAsync(new Event { Id = Guid.NewGuid() }));

        Assert.Equal(HttpStatusCode.BadGateway, exception.StatusCode);
        Assert.Equal(1, handler.CallCount);
    }

    private sealed class TestHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHttpMessageHandler(
        string responseBody,
        HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });
        }
    }
}
