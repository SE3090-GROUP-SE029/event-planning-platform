using System.Diagnostics;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.GuestManagement;

public class RegistrationLinkEmailDeliveryService(
    IGuestRegistrationRepository repository,
    IRegistrationLinkEmailSender emailSender,
    TimeProvider clock,
    ILogger<RegistrationLinkEmailDeliveryService> logger)
{
    private static readonly TimeSpan JobLease = TimeSpan.FromMinutes(2);

    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        var claimStarted = Stopwatch.GetTimestamp();
        var job = await repository.ClaimNextRegistrationLinkEmailJobAsync(
            clock.GetUtcNow(), JobLease, cancellationToken);
        if (job is null) return false;

        logger.LogInformation(
            "Registration-link email sending started. JobId={JobId}, EventId={EventId}, Attempt={Attempt}, ClaimElapsedMs={ClaimElapsedMs}",
            job.Id,
            job.EventId,
            job.AttemptCount,
            Stopwatch.GetElapsedTime(claimStarted).TotalMilliseconds);

        var sendStarted = Stopwatch.GetTimestamp();
        EmailDeliveryResult delivery;
        try
        {
            delivery = await emailSender.SendRegistrationLinkAsync(
                new RegistrationLinkEmail(
                    job.EmailAddress,
                    job.FullName,
                    job.EventName,
                    job.RegistrationUrl),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Registration-link email job threw an exception. JobId={JobId}, EventId={EventId}",
                job.Id,
                job.EventId);
            delivery = EmailDeliveryResult.FAILED;
        }

        var sendElapsed = Stopwatch.GetElapsedTime(sendStarted);
        await repository.CompleteRegistrationLinkEmailJobAsync(
            job.Id, delivery, clock.GetUtcNow(), cancellationToken);
        logger.LogInformation(
            "Registration-link email sending completed. JobId={JobId}, EventId={EventId}, Result={Result}, Attempt={Attempt}, ElapsedMs={ElapsedMs}",
            job.Id,
            job.EventId,
            delivery,
            job.AttemptCount,
            sendElapsed.TotalMilliseconds);
        return true;
    }
}
