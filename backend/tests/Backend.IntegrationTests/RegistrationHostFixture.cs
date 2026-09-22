using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using Api.Controllers;
using Api.GuestManagement;
using Application.GuestManagement;
using Application.Services;
using Application.TestSerivce;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.ExternalServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Backend.IntegrationTests;

public class RegistrationHostFixture : IAsyncLifetime
{
    private WebApplication? app;
    private string dataDirectory = string.Empty;
    private string postgresBin = string.Empty;
    private bool databaseStarted;
    public HttpClient Client { get; private set; } = null!;
    public string ConnectionString { get; private set; } = string.Empty;
    public TestClock Clock { get; } = new();
    public RecordingEmailSender Email { get; } = new();
    public RecordingAiClient Ai { get; } = new();
    public static readonly DateTimeOffset Now = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public async Task InitializeAsync()
    {
        // Test-only isolated cluster. No configured application database or SMTP provider is used.
        postgresBin = Environment.GetEnvironmentVariable("TEST_POSTGRES_BIN") ?? @"C:\Program Files\PostgreSQL\18\bin";
        dataDirectory = Path.Combine(AppContext.BaseDirectory, "postgres-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dataDirectory);
        var port = FreePort();
        await RunPostgresToolAsync("initdb", "-D", dataDirectory, "-A", "trust", "-U", "postgres", "--no-locale", "--encoding=UTF8");
        await RunPostgresToolAsync("pg_ctl", "-D", dataDirectory, "-l", Path.Combine(dataDirectory, "server.log"),
            "-o", $"-h 127.0.0.1 -p {port}", "-w", "-t", "30", "start");
        databaseStarted = true;
        ConnectionString = new NpgsqlConnectionStringBuilder
        {
            Host = "127.0.0.1", Port = port, Database = "postgres", Username = "postgres",
            Timeout = 10, CommandTimeout = 30, MaxPoolSize = 30
        }.ConnectionString;
        await using (var db = CreateDb()) await db.Database.MigrateAsync();

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Error);
        builder.Services.AddControllers().AddApplicationPart(typeof(RegistrationFormsController).Assembly);
        builder.Services.AddGuestManagement(builder.Configuration);
        builder.Services.AddScoped<ITestService, TestSerivce>();
        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(ConnectionString));
        builder.Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(Clock));
        builder.Services.Replace(ServiceDescriptor.Singleton<IInvitationEmailSender>(Email));
        // Drive durable work explicitly in tests; no test calls Python or Ollama.
        builder.Services.Replace(ServiceDescriptor.Singleton(new AiClientOptions { Enabled = false }));
        builder.Services.Replace(ServiceDescriptor.Singleton<IGuestAiClient>(Ai));
        builder.Services.Replace(ServiceDescriptor.Singleton<IRegistrationQuestionClient>(Ai));
        app = builder.Build();
        // This middleware exists only in the test assembly. Production never trusts this header.
        app.Use(async (context, next) =>
        {
            if (context.Request.Headers.TryGetValue("X-Test-Identity", out var identity))
            {
                var role = context.Request.Headers["X-Test-Role"].FirstOrDefault() ?? "Planner";
                context.User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, identity.ToString()), new Claim(ClaimTypes.Role, role)], "TestOnly"));
            }
            await next(context);
        });
        app.MapControllers();
        await app.StartAsync();
        Client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()), Timeout = TimeSpan.FromSeconds(60) };
    }

    public AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options);

    public static Guid GuestOwnerGuid(string owner) =>
        new Guid(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(owner)));
    public static readonly string PlannerId = GuestOwnerGuid("planner").ToString();

    public async Task<Event> CreateEventAsync(string owner = "planner")
    {
        Clock.UtcNow = Now;
        Email.Result = EmailDeliveryResult.SENT;
        Email.ThrowOnSend = false;
        var eventDetails = new Event
        {
            OwnerId = GuestOwnerGuid(owner), EventName = "Guest Management Integration Event", Requirements = "Test description",
            PreferredVenue = "Colombo", PreferredDate = Now.AddDays(10).UtcDateTime, EventDuration = TimeSpan.FromHours(3),
            CreatedAt = Now.UtcDateTime, UpdatedAt = Now.UtcDateTime
        };
        await using var db = CreateDb();
        db.Events.Add(eventDetails);
        await db.SaveChangesAsync();
        return eventDetails;
    }

    public async Task<T> WithServiceAsync<T>(Func<RegistrationService, Task<T>> action)
    {
        using var scope = app!.Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<RegistrationService>());
    }

    public async Task<T> WithAiServiceAsync<T>(Func<GuestAiReviewService, Task<T>> action)
    {
        using var scope = app!.Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<GuestAiReviewService>());
    }

    public async Task<T> WithAiRepositoryAsync<T>(Func<IGuestAiReviewRepository, Task<T>> action)
    {
        using var scope = app!.Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<IGuestAiReviewRepository>());
    }

    public async Task DrainAiAsync()
    {
        for (var i = 0; i < 1000; i++)
            if (!await WithAiServiceAsync(s => s.ProcessNextAsync(TimeSpan.FromMinutes(3), CancellationToken.None))) return;
        throw new InvalidOperationException("Unexpected AI or delivery backlog");
    }

    public async Task ApplyDecisionAsync(GuestAiClaim claim, GuestAiDecision decision)
    {
        using var scope = app!.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<RegistrationService>().ApplyDecisionAsync(claim, decision,
            scope.ServiceProvider.GetRequiredService<IGuestAiReviewRepository>(), CancellationToken.None);
    }

    public async Task<RegistrationSubmission> AcceptAsync(RegistrationReceipt receipt)
    {
        await DrainAiAsync();
        return await WithServiceAsync(s => s.GetPublicStatusAsync(receipt.Registration.PublicReference, receipt.StatusSecret, CancellationToken.None));
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (app is not null) await app.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        if (databaseStarted) await RunPostgresToolAsync("pg_ctl", "-D", dataDirectory, "-m", "fast", "-w", "stop");
        // Preserve cluster files/logs for inspection. No database or directory deletion occurs.
    }

    private async Task RunPostgresToolAsync(string name, params string[] arguments)
    {
        var executable = Path.Combine(postgresBin, OperatingSystem.IsWindows() ? name + ".exe" : name);
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Cannot start {name}.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(60));
        if (process.ExitCode != 0) throw new InvalidOperationException($"{name} failed: {await output}\n{await error}");
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

public class TestClock : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = RegistrationHostFixture.Now;
    public override DateTimeOffset GetUtcNow() => UtcNow;
}

public class RecordingEmailSender : IInvitationEmailSender
{
    public ConcurrentQueue<RejectionEmail> Rejections { get; } = new();
    public ConcurrentQueue<InvitationEmail> Messages { get; } = new();
    public EmailDeliveryResult Result { get; set; } = EmailDeliveryResult.SENT;
    public bool ThrowOnSend { get; set; }
    public Task<EmailDeliveryResult> SendAsync(InvitationEmail invitation, CancellationToken cancellationToken)
    {
        Messages.Enqueue(invitation);
        if (ThrowOnSend) throw new InvalidOperationException("Test transport failure");
        return Task.FromResult(Result);
    }
    public Task<EmailDeliveryResult> SendRejectionAsync(RejectionEmail rejection, CancellationToken cancellationToken)
    {
        Rejections.Enqueue(rejection);
        if (ThrowOnSend) throw new InvalidOperationException("Test transport failure");
        return Task.FromResult(Result);
    }
}

public class RecordingAiClient : IGuestAiClient, IRegistrationQuestionClient
{
    public ConcurrentQueue<AiEventContext> QuestionContexts { get; } = new();
    public Task<QuestionSuggestions> SuggestAsync(AiEventContext context, CancellationToken ct)
    {
        QuestionContexts.Enqueue(context);
        if (Failure is not null) throw Failure;
        return Task.FromResult(new QuestionSuggestions([new("Why would you like to attend?", false)]));
    }
    public ConcurrentQueue<GuestAiContext> Contexts { get; } = new();
    public GuestAiDecision Decision { get; set; } = new(Domain.Enums.AiDecision.ACCEPTED, 0.9, ["No issue found in supplied data."], [], "qwen3:8b", "guest-filtering-v2");
    public Exception? Failure { get; set; }
    public Func<CancellationToken, Task>? BeforeReturn { get; set; }

    public async Task<GuestAiDecision> AnalyzeAsync(GuestAiContext context, CancellationToken ct)
    {
        Contexts.Enqueue(context);
        if (BeforeReturn is not null) await BeforeReturn(ct);
        if (Failure is not null) throw Failure;
        return Decision;
    }
}
