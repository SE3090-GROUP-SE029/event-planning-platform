using System.Net;
using System.Text;
using System.Text.Json;
using Application.GuestManagement;
using Domain.Enums;
using Infrastructure.ExternalServices;

namespace Backend.UnitTests;

public class GuestAiReviewTests
{
    private static readonly GuestAiContext Context = new(new("Guest", "guest@example.com", null, null),
        new("Event", null), DateTimeOffset.UtcNow, [], false);

    private static string Result(string recommendation = "ACCEPTED", object? confidence = null,
        string[]? reasons = null, string[]? flags = null) => JsonSerializer.Serialize(new
        {
            decision = recommendation, confidence = confidence ?? 0.9, reasons = reasons ?? ["Supplied information is consistent."],
            flags = flags ?? [], model = "qwen3:8b", promptVersion = "guest-filtering-v2"
        });

    private static AiClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send, int timeout = 120)
        => new(new HttpClient(new FakeHandler(send)) { Timeout = Timeout.InfiniteTimeSpan }, new AiClientOptions { TimeoutSeconds = timeout });

    private static AiClient Client(string body, HttpStatusCode status = HttpStatusCode.OK)
        => Client((_, _) => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") }));

    [Theory]
    [InlineData("ACCEPTED")]
    [InlineData("REJECTED")]
    public async Task AcceptsOnlyStructuredRecommendations(string recommendation)
    {
        var decision = await Client(Result(recommendation)).AnalyzeAsync(Context, CancellationToken.None);
        Assert.Equal(recommendation, decision.Decision.ToString());
        Assert.Equal(0.9, decision.Confidence);
        Assert.Equal("qwen3:8b", decision.Model);
    }

    public static IEnumerable<object[]> MalformedResponses()
    {
        foreach (var body in new[] { "not json", "```json\n{}\n```", "{}", "[]", "null", Result("APPROVED"), Result("1"),
            Result("ELIGIBLE"), Result("REVIEW"),
            Result(confidence: 1.1), Result(confidence: -0.1), Result(confidence: "0.9"), Result(confidence: true),
            Result(reasons: []), Result(reasons: [" "]), Result(reasons: ["bad\ntext"]),
            Result(reasons: [new string('x', 501)]), Result(flags: [new string('x', 81)]),
            Result().Replace("0.9", "1e999"), Result().Replace("0.9", "null"),
            Result().Replace("\"confidence\":0.9", "\"confidence\":0.9,\"confidence\":0.1"),
            Result().Replace("\"model\":", "\"extra\":true,\"model\":"),
            new string('x', 16385) }) yield return [body];
    }

    [Theory]
    [MemberData(nameof(MalformedResponses))]
    public async Task RejectsMalformedUnboundedOrAmbiguousOutput(string body)
        => Assert.Equal("invalid_ai_response", (await Assert.ThrowsAsync<AiAnalysisException>(() =>
            Client(body).AnalyzeAsync(Context, CancellationToken.None))).Code);

    [Fact]
    public async Task ConnectionFailureIsSanitized()
    {
        var client = Client((_, _) => throw new HttpRequestException("sensitive provider details"));
        var error = await Assert.ThrowsAsync<AiAnalysisException>(() => client.AnalyzeAsync(Context, CancellationToken.None));
        Assert.Equal("ai_unavailable", error.Message);
        Assert.Null(error.InnerException);
    }

    [Fact]
    public async Task BoundedTimeoutCancelsTransport()
    {
        var client = Client(async (_, ct) => { await Task.Delay(Timeout.Infinite, ct); return new HttpResponseMessage(); }, 1);
        Assert.Equal("ai_timeout", (await Assert.ThrowsAsync<AiAnalysisException>(() => client.AnalyzeAsync(Context, CancellationToken.None))).Code);
    }

    [Fact]
    public async Task ShutdownCancellationIsNotConvertedToGuestFailure()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var client = Client((_, ct) => Task.FromCanceled<HttpResponseMessage>(ct));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.AnalyzeAsync(Context, cancelled.Token));
    }

    [Theory]
    [InlineData(503, "ai_unavailable")]
    [InlineData(504, "ai_timeout")]
    [InlineData(502, "invalid_ai_response")]
    [InlineData(302, "ai_unavailable")]
    public async Task ServiceErrorsNeverBecomeDecisions(int status, string code)
        => Assert.Equal(code, (await Assert.ThrowsAsync<AiAnalysisException>(() =>
            Client("private error body", (HttpStatusCode)status).AnalyzeAsync(Context, CancellationToken.None))).Code);

    [Fact]
    public async Task SendsOnlyAllowlistedContextAndUsesLocalService()
    {
        var client = Client(async (request, ct) =>
        {
            Assert.True(request.RequestUri!.IsLoopback);
            Assert.Equal("/api/guest-reviews/analyze", request.RequestUri.AbsolutePath);
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            Assert.Equal(new[] { "comparisons", "comparisonsLimited", "event", "guest", "questions", "registeredAt" }, json.RootElement.EnumerateObject().Select(p => p.Name).Order());
            Assert.Equal(4, json.RootElement.GetProperty("guest").EnumerateObject().Count());
            Assert.Equal(2, json.RootElement.GetProperty("event").EnumerateObject().Count());
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Result()) };
        });
        await client.AnalyzeAsync(Context, CancellationToken.None);
    }

    [Theory]
    [InlineData("https://example.com/")]
    [InlineData("http://user:secret@localhost/")]
    [InlineData("http://localhost/?key=secret")]
    public async Task InvalidServiceConfigurationDoesNotSendData(string url)
    {
        var client = new AiClient(new HttpClient(new FakeHandler((_, _) => throw new Exception("Must not send"))), new AiClientOptions { ServiceUrl = url });
        Assert.Equal("ai_unavailable", (await Assert.ThrowsAsync<AiAnalysisException>(() => client.AnalyzeAsync(Context, CancellationToken.None))).Code);
    }

    private class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request, ct);
    }

    [Theory]
    [InlineData("{\"questions\":[]}")]
    [InlineData("{\"questions\":[{\"question\":\"x\",\"required\":\"true\"}]}")]
    [InlineData("{\"questions\":[{\"question\":\"x\",\"required\":true,\"required\":false}]}")]
    [InlineData("{\"questions\":[{\"question\":\"x\",\"required\":true,\"extra\":true}]}")]
    public async Task InvalidQuestionSuggestionsFailStrictValidation(string body)
        => Assert.Equal("invalid_ai_response", (await Assert.ThrowsAsync<AiAnalysisException>(() =>
            Client(body).SuggestAsync(new("Event", null), CancellationToken.None))).Code);

    [Fact]
    public async Task QuestionGenerationSendsOnlyEventAndKeepsBooleanRequiredFlag()
    {
        var client = Client(async (request, ct) =>
        {
            Assert.Equal("/api/registration-questions/suggest", request.RequestUri!.AbsolutePath);
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            Assert.Equal("event", Assert.Single(json.RootElement.EnumerateObject()).Name);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"questions\":[{\"question\":\"Why attend?\",\"required\":false}]}") };
        });
        var result = await client.SuggestAsync(new("Event", null), CancellationToken.None);
        Assert.False(Assert.Single(result.Questions).Required);
    }
}
