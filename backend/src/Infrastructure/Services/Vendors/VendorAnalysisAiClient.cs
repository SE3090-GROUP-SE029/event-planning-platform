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
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            string? aiMessage;
            try
            {
                aiMessage = ReadPublicErrorMessage(responseBody);
            }
            catch (JsonException exception)
            {
                logger.LogWarning(
                    exception,
                    "Vendor analysis AI returned an unparseable error response for event {EventId}",
                    request.EventId);
                aiMessage = null;
            }
            logger.LogWarning(
                "Vendor analysis AI returned HTTP {StatusCode} for event {EventId}; message {AiMessage}",
                (int)response.StatusCode,
                request.EventId,
                aiMessage);

            var status = response.StatusCode;
            if (status == System.Net.HttpStatusCode.GatewayTimeout ||
                status == System.Net.HttpStatusCode.RequestTimeout)
            {
                throw new TaskCanceledException("Vendor recommendation timed out.");
            }

            throw new HttpRequestException(
                aiMessage ?? $"Vendor analysis AI returned {(int)status}.",
                null,
                status);
        }

        var payload = await response.Content.ReadFromJsonAsync<VendorAnalysisRecommendResponse>(
            SerializerOptions,
            cancellationToken);

        return payload ?? throw new InvalidOperationException("Vendor analysis AI returned an empty response.");
    }

    private static string? ReadPublicErrorMessage(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return null;

        using var document = JsonDocument.Parse(responseBody);
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty("detail", out var detail))
            return null;

        var message = detail.ValueKind switch
        {
            JsonValueKind.String => detail.GetString(),
            JsonValueKind.Object when detail.TryGetProperty("message", out var value) &&
                                      value.ValueKind == JsonValueKind.String =>
                value.GetString(),
            _ => null
        };
        if (string.IsNullOrWhiteSpace(message))
            return null;

        var normalized = string.Join(" ", message.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= 1000 ? normalized : normalized[..1000];
    }
}
