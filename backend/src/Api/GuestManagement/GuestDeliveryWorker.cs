using Application.GuestManagement;

namespace Api.GuestManagement;

public class GuestDeliveryWorker(
    IServiceScopeFactory scopes,
    ILogger<GuestDeliveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var invitations = scope.ServiceProvider.GetRequiredService<InvitationService>();
                if (await invitations.ProcessPendingDeliveryAsync(stoppingToken)) continue;
                var reviews = scope.ServiceProvider.GetRequiredService<RegistrationReviewService>();
                if (await reviews.ProcessPendingRejectionEmailAsync(stoppingToken)) continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning("Guest email delivery worker failed ({FailureType}); queued messages will be retried",
                    exception.GetType().Name);
            }
            try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
