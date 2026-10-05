using Application.GuestManagement;

namespace Api.GuestManagement;

public class RegistrationLinkEmailWorker(
    IServiceScopeFactory scopes,
    ILogger<RegistrationLinkEmailWorker> logger) : BackgroundService
{
    private const int WorkerCount = 4;
    private static readonly TimeSpan IdleDelay = TimeSpan.FromMilliseconds(500);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.WhenAll(Enumerable.Range(0, WorkerCount)
            .Select(workerId => RunWorkerAsync(workerId, stoppingToken)));

    private async Task RunWorkerAsync(int workerId, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<RegistrationLinkEmailDeliveryService>();
                if (await processor.ProcessNextAsync(stoppingToken)) continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Registration-link email worker failed. WorkerId={WorkerId}; durable jobs will be retried",
                    workerId);
            }

            try
            {
                await Task.Delay(IdleDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
