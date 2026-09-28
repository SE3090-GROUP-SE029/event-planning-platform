using System.Net.Http.Json;
using System.Text.Json;
using Application.Dtos.Vendors;
using Application.Services.Vendors;
using Infrastructure.Services.Planning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Vendors;

public sealed class VendorAnalysisAiClient(
    IHttpClientFactory httpClientFactory,
    IOptions<AgenticAiOptions> options,
    ILogger<VendorAnalysisAiClient> logger) : IVendorAnalysisAiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<VendorAnalysisRecommendResponse> RecommendAsync(
        VendorAnalysisRecommendRequest request,
        CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("AgenticAI");
        var path = string.IsNullOrWhiteSpace(options.Value.VendorRecommendPath)
            ? "/api/vendor-analysis/recommend"
            : options.Value.VendorRecommendPath;

        using var response = await client.PostAsJsonAsync(path, request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Vendor analysis AI returned HTTP {StatusCode} for event {EventId}",
                (int)response.StatusCode,
                request.EventId);

            var status = response.StatusCode;
            if (status == System.Net.HttpStatusCode.GatewayTimeout ||
                status == System.Net.HttpStatusCode.RequestTimeout)
            {
                throw new TaskCanceledException("Vendor recommendation timed out.");
            }

            throw new HttpRequestException(
                $"Vendor analysis AI returned {(int)status}.",
                null,
                status);
        }

        var payload = await response.Content.ReadFromJsonAsync<VendorAnalysisRecommendResponse>(
            SerializerOptions,
            cancellationToken);

        return payload ?? throw new InvalidOperationException("Vendor analysis AI returned an empty response.");
    }
}
