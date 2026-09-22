using Application.GuestManagement;
using Infrastructure.ExternalServices;
using Infrastructure.Repositories;

namespace Api.GuestManagement;

public static class GuestManagementServices
{
    public static IServiceCollection AddGuestManagement(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IRegistrationTokenGenerator, RegistrationTokenGenerator>();
        services.AddScoped<IRegistrationEligibilityPolicy, ValidatedRegistrationEligibilityPolicy>();
        services.AddScoped<IGuestRegistrationRepository, GuestRegistrationRepository>();
        services.AddScoped<RegistrationService>();
        services.AddSingleton(configuration.GetSection("GuestAi").Get<AiClientOptions>() ?? new AiClientOptions());
        services.AddHttpClient<IGuestAiClient, AiClient>(client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false });
        services.AddHttpClient<IRegistrationQuestionClient, AiClient>(client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false });
        services.AddScoped<IGuestAiReviewRepository, GuestAiReviewRepository>();
        services.AddScoped<GuestAiReviewService>();
        services.AddHostedService<GuestAiReviewWorker>();
        services.AddSingleton(configuration.GetSection("Smtp").Get<SmtpEmailOptions>() ?? new SmtpEmailOptions());
        services.AddScoped<IInvitationEmailSender, EmailSender>();
        services.AddScoped<PlannerAccessFilter>();
        services.AddScoped<RegistrationExceptionFilter>();
        return services;
    }
}
