using Application.GuestManagement;
using Infrastructure.ExternalServices;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;

namespace Api.GuestManagement;

public static class GuestManagementServices
{
    public static IServiceCollection AddGuestManagement(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IRegistrationTokenGenerator, RegistrationTokenGenerator>();
        services.AddDataProtection();
        services.AddSingleton<IRegistrationSecretProtector, RegistrationSecretProtector>();
        var guestRegistrationOptions =
            configuration.GetSection("GuestRegistration").Get<GuestRegistrationOptions>()
            ?? new GuestRegistrationOptions();
        guestRegistrationOptions.AllowInsecureLocalhost = environment.IsDevelopment();
        services.AddSingleton(guestRegistrationOptions);
        services.AddScoped<IRegistrationEligibilityPolicy, ValidatedRegistrationEligibilityPolicy>();
        services.AddScoped<IGuestRegistrationRepository, GuestRegistrationRepository>();
        services.AddScoped<InvitationService>();
        services.AddScoped<SeatAllocationService>();
        services.AddScoped<RegistrationReviewService>();
        services.AddScoped<RegistrationRsvpService>();
        services.AddScoped<GuestCheckInService>();
        services.AddScoped<RegistrationService>();
        services.AddScoped<BulkGuestUploadService>(); // C4 planner guest list upload
        services.AddScoped<RegistrationLinkEmailDeliveryService>();
        services.Configure<GuestAiOptions>(configuration.GetSection("GuestAi"));
        services.PostConfigure<GuestAiOptions>(options =>
            options.TimeoutSeconds = Math.Max(
                options.TimeoutSeconds,
                AiClientOptions.RequestTimeoutSeconds));
        services.AddSingleton<AiClientOptions>(provider =>
            provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<GuestAiOptions>>().Value);
        services.AddHttpClient<IGuestAiClient, AiClient>(client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false });
        services.AddHttpClient<IRegistrationQuestionClient, AiClient>(client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false });
        services.AddScoped<IGuestAiReviewRepository, GuestAiReviewRepository>();
        services.AddScoped<GuestAiReviewService>();
        if (!environment.IsEnvironment("Testing"))
        {
            services.AddHostedService<GuestAiReviewWorker>();
            services.AddHostedService<GuestSeatAllocationWorker>();
            services.AddHostedService<GuestInvitationWorker>();
            services.AddHostedService<GuestDeliveryWorker>();
            services.AddHostedService<RegistrationLinkEmailWorker>();
        }
        services.Configure<SmtpOptions>(configuration.GetSection("Smtp"));
        services.AddSingleton<SmtpEmailOptions>(provider =>
            provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SmtpOptions>>().Value);
        services.AddScoped<EmailSender>();
        services.AddScoped<IInvitationEmailSender>(provider => provider.GetRequiredService<EmailSender>());
        services.AddScoped<IRegistrationOutcomeEmailSender>(provider => provider.GetRequiredService<EmailSender>());
        services.AddScoped<IRegistrationLinkEmailSender>(provider => provider.GetRequiredService<EmailSender>());
        services.AddScoped<PlannerAccessFilter>();
        services.AddScoped<RegistrationExceptionFilter>();
        return services;
    }
}
