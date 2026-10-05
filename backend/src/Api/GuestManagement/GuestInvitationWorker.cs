using Application.GuestManagement;

namespace Api.GuestManagement;

public class GuestInvitationWorker(
    IServiceScopeFactory scopes,
    ILogger<GuestInvitationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var invitations = scope.ServiceProvider.GetRequiredService<InvitationService>();
                if (await invitations.ProcessNextGenerationAsync(stoppingToken)) continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning("Invitation generation worker failed ({FailureType}); confirmed guests will be retried",
                    exception.GetType().Name);
            }
            try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
