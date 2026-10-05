using Application.GuestManagement;

namespace Api.GuestManagement;

public class GuestSeatAllocationWorker(
    IServiceScopeFactory scopes,
    ILogger<GuestSeatAllocationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var allocator = scope.ServiceProvider.GetRequiredService<SeatAllocationService>();
                if (await allocator.ProcessNextAsync(stoppingToken)) continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning("Seat allocation worker failed ({FailureType}); queued work will be retried",
                    exception.GetType().Name);
            }
            try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
