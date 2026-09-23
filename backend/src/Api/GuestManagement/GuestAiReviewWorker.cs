using Application.GuestManagement;
using Infrastructure.ExternalServices;

namespace Api.GuestManagement;

public class GuestAiReviewWorker(IServiceScopeFactory scopes, AiClientOptions options,
    ILogger<GuestAiReviewWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled) return;
        if (!options.IsValid)
        {
            logger.LogWarning("AI review configuration is invalid; reviews remain pending until configuration is corrected and the backend restarted");
            return;
        }
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<GuestAiReviewService>();
                if (await service.ProcessNextAsync(TimeSpan.FromSeconds(options.TimeoutSeconds + 30), stoppingToken)) continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                // No exception message/stack: provider exceptions may contain connection details or input.
                logger.LogWarning("AI review worker could not process work ({FailureType}); durable work will be retried", exception.GetType().Name);
            }
            try { await Task.Delay(TimeSpan.FromSeconds(options.PollSeconds), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
