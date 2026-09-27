using System.Collections.Concurrent;
using System.Security.Claims;
using Api.Controllers;
using Api.GuestManagement;
using Application.GuestManagement;
using Application.Services;
using Application.Services.Test;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.ExternalServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Backend.IntegrationTests;

public class RegistrationHostFixture : IAsyncLifetime
{
    private WebApplication? app;
    private string schemaName = string.Empty;
    public HttpClient Client { get; private set; } = null!;
    public string ConnectionString { get; private set; } = string.Empty;
    public TestClock Clock { get; } = new();
    public RecordingEmailSender Email { get; } = new();
    public RecordingAiClient Ai { get; } = new();
    public static readonly DateTimeOffset Now = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public async Task InitializeAsync()
    {
        var configuredConnectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION");
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            configuredConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        }

        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            throw new InvalidOperationException(
                "Set TEST_DATABASE_CONNECTION or ConnectionStrings__DefaultConnection to a PostgreSQL test database.");
        }

        var connectionBuilder = new NpgsqlConnectionStringBuilder(configuredConnectionString)
        {
            Timeout = 10, CommandTimeout = 30, MaxPoolSize = 30
        };
        schemaName = $"integration_{Guid.NewGuid():N}";
        await using (var connection = new NpgsqlConnection(connectionBuilder.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE SCHEMA \"{schemaName}\"";
            await command.ExecuteNonQueryAsync();
        }

        connectionBuilder.SearchPath = schemaName;
        ConnectionString = connectionBuilder.ConnectionString;
        await using (var db = CreateDb()) await db.Database.MigrateAsync();

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
            ContentRootPath = Path.GetTempPath()
        });
        // Keep test-host settings independent of developer-specific appsettings files.
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddEnvironmentVariables();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Error);
        builder.Services.AddControllers().AddApplicationPart(typeof(RegistrationFormsController).Assembly);
        builder.Services.AddGuestManagement(builder.Configuration);
        builder.Services.AddScoped<ITestService, TestService>();
        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(
            ConnectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schemaName)));
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
                var role = context.Request.Headers["X-Test-Role"].FirstOrDefault() ?? "EVENT_PLANNER";
                context.User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, identity.ToString()), new Claim(ClaimTypes.Role, role)], "TestOnly"));
            }
            await next(context);
        });
        app.MapControllers();
        await app.StartAsync();
        Client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()), Timeout = TimeSpan.FromSeconds(60) };
    }

    public AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(
            ConnectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schemaName))
        .Options);

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
            OwnerId = GuestOwnerGuid(owner), EventName = "Guest Management Integration Event",
            EventType = EventType.CORPORATE, GuestCount = 1, Budget = 1000,
            Requirements = "Test description", PreferredVenue = "Colombo",
            PreferredDate = Now.AddDays(10).UtcDateTime, EventDuration = TimeSpan.FromHours(3),
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
    public GuestAiDecision Decision { get; set; } = new(Domain.Enums.AiDecision.ACCEPTED, 0.9, ["No issue found in supplied data."], [], "gemini-3.8-flash", "guest-filtering-v2");
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
