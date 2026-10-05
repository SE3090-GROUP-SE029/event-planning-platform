using Application.Services.Vendors;

namespace Api.Recommendations;

public sealed class VendorRecommendationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<VendorRecommendationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var recommendations =
                    scope.ServiceProvider.GetRequiredService<IVendorRecommendationService>();
                if (await recommendations.ProcessNextAsync(stoppingToken))
                    continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Vendor recommendation worker iteration failed, failure type {FailureType}",
                    exception.GetType().Name);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
