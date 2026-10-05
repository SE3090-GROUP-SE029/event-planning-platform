using System.Net;
using System.Net.Http.Json;
using Application.Dtos.Vendors;
using Infrastructure.Services.Planning;
using Infrastructure.Services.Vendors;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Backend.UnitTests;

public class VendorAnalysisAiClientTests
{
    [Fact]
    public async Task RecommendAsync_PreservesPublicAiErrorMessageAndStatus()
    {
        const string aiMessage = "The AI provider could not rank vendors. Please try again later.";
        var handler = new StubHandler(
            new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = JsonContent.Create(new
                {
                    detail = new { code = "provider_error", message = aiMessage }
                })
            });
        var client = new VendorAnalysisAiClient(
            new StubHttpClientFactory(handler),
            Options.Create(new AgenticAiOptions
            {
                BaseUrl = "http://localhost:8000"
            }),
            NullLogger<VendorAnalysisAiClient>.Instance);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.RecommendAsync(new VendorAnalysisRecommendRequest
            {
                EventId = Guid.NewGuid(),
                PlanId = Guid.NewGuid()
            }));

        Assert.Equal(HttpStatusCode.BadGateway, exception.StatusCode);
        Assert.Equal(aiMessage, exception.Message);
    }

    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(handler, disposeHandler: false)
            {
                BaseAddress = new Uri("http://localhost:8000")
            };
    }
}
