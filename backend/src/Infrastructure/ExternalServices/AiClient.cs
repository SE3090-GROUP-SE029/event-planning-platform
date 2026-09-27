using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.GuestManagement;
using Domain.Enums;

namespace Infrastructure.ExternalServices;

public class AiClient(HttpClient http, AiClientOptions options) : IGuestAiClient, IRegistrationQuestionClient
{
    private const int MaximumResponseBytes = 16_384;

    public async Task<GuestAiDecision> AnalyzeAsync(GuestAiContext context, CancellationToken ct)
        => Parse(await SendAsync("api/guest-reviews/analyze", context with { Questions = context.Questions ?? [] }, ct));

    public async Task<QuestionSuggestions> SuggestAsync(AiEventContext context, CancellationToken ct)
    {
        var bytes = await SendAsync("api/registration-questions/suggest", new { Event = context }, ct);
        try
        {
            using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            RequireProperties(root, ["questions"]);
            var questions = root.GetProperty("questions").EnumerateArray().Select(q =>
            {
                RequireProperties(q, ["question", "required"]);
                return new QuestionSelection(q.GetProperty("question").GetString()!, q.GetProperty("required").GetBoolean());
            }).ToArray();
            if (questions.Length == 0) throw new AiAnalysisException("invalid_ai_response");
            return new QuestionSuggestions(RegistrationValidator.ValidateQuestions(questions));
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException or KeyNotFoundException or RegistrationException)
        { throw new AiAnalysisException("invalid_ai_response"); }
    }

    private async Task<ReadOnlyMemory<byte>> SendAsync(string path, object context, CancellationToken ct)
    {
        if (!options.IsValid) throw new AiAnalysisException("ai_unavailable");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(options.ServiceUrl), path))
            { Content = JsonContent.Create(context) };
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (response.StatusCode == HttpStatusCode.GatewayTimeout) throw new AiAnalysisException("ai_timeout");
            if (!response.IsSuccessStatusCode)
                throw new AiAnalysisException(response.StatusCode == HttpStatusCode.BadGateway ? "invalid_ai_response" : "ai_unavailable");
            if (response.Content.Headers.ContentLength > MaximumResponseBytes) throw new AiAnalysisException("invalid_ai_response");
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            var bytes = new byte[MaximumResponseBytes + 1];
            var count = 0;
            while (count < bytes.Length)
            {
                var read = await stream.ReadAsync(bytes.AsMemory(count), timeout.Token);
                if (read == 0) break;
                count += read;
            }
            if (count > MaximumResponseBytes) throw new AiAnalysisException("invalid_ai_response");
            return bytes.AsMemory(0, count);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { throw new AiAnalysisException("ai_timeout"); }
        catch (HttpRequestException) { throw new AiAnalysisException("ai_unavailable"); }
        catch (IOException) { throw new AiAnalysisException("ai_unavailable"); }
    }

    private static GuestAiDecision Parse(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            RequireProperties(root, ["decision", "confidence", "reasons", "flags", "model", "promptVersion"]);
            var value = root.GetProperty("decision").GetString();
            if (!Enum.GetNames<AiDecision>().Contains(value)) throw new AiAnalysisException("invalid_ai_response");
            var decision = new GuestAiDecision(Enum.Parse<AiDecision>(value!), root.GetProperty("confidence").GetDouble(),
                root.GetProperty("reasons").EnumerateArray().Select(v => v.GetString()!).ToArray(),
                root.GetProperty("flags").EnumerateArray().Select(v => v.GetString()!).ToArray(),
                root.GetProperty("model").GetString()!, root.GetProperty("promptVersion").GetString()!);
            decision.Validate();
            return decision;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException or KeyNotFoundException)
        { throw new AiAnalysisException("invalid_ai_response"); }
    }

    private static void RequireProperties(JsonElement element, string[] expected)
    {
        var properties = element.EnumerateObject().Select(p => p.Name).ToArray();
        if (properties.Length != expected.Length || !properties.Order().SequenceEqual(expected.Order()))
            throw new AiAnalysisException("invalid_ai_response");
    }
}
