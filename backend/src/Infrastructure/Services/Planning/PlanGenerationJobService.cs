using Application.Common.Interfaces;
using Application.Services.Planning;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Diagnostics;

namespace Infrastructure.Services.Planning;

public sealed class PlanGenerationJobService(
    IEventRepository eventRepository,
    IEventPlanDraftRepository planRepository,
    IPlanGenerationJobRepository jobRepository,
    ICurrentUserService currentUser,
    IPlanGenerationService generationService,
    ILogger<PlanGenerationJobService> logger) : IPlanGenerationJobService
{
    private static readonly TimeSpan ProcessingLease = TimeSpan.FromMinutes(31);

    public async Task<PlanGenerationJob> EnqueueAsync(
        Guid eventId,
        bool regenerate,
        string requestId,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("User identity is missing.");
        var eventEntity = await eventRepository.GetByIdAsync(eventId)
            ?? throw new KeyNotFoundException("Event not found.");
        EnsureAuthorized(eventEntity, userId);

        var activeJob = await jobRepository.GetActiveForEventAsync(eventId, cancellationToken);
        if (activeJob is not null)
        {
            logger.LogInformation(
                "Returning existing plan generation job {GenerationJobId} for event {EventId}, request {RequestId}",
                activeJob.Id,
                eventId,
                requestId);
            return activeJob;
        }

        var existingPlans = await planRepository.ListAsync(eventId, null, null, cancellationToken);
        if (existingPlans.Count > 0 && !regenerate)
            throw new InvalidOperationException("A plan already exists. Review it or explicitly regenerate it.");
        if (regenerate && existingPlans.FirstOrDefault()?.Status != PlanStatus.Rejected)
            throw new InvalidOperationException("Only a rejected plan can be regenerated.");

        var now = DateTime.UtcNow;
        var job = new PlanGenerationJob
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            RequestedById = userId,
            RequestId = requestId,
            Regenerate = regenerate,
            Status = PlanGenerationJobStatus.Queued,
            CreatedAt = now,
            UpdatedAt = now
        };

        await jobRepository.AddAsync(job, cancellationToken);
        try
        {
            await jobRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            var concurrentJob = await jobRepository.GetActiveForEventAsync(eventId, cancellationToken);
            if (concurrentJob is null) throw;
            return concurrentJob;
        }

        logger.LogInformation(
            "Plan generation job queued for event {EventId}, generation job {GenerationJobId}, request {RequestId}",
            eventId,
            job.Id,
            requestId);
        return job;
    }

    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var staleBefore = now - ProcessingLease;
        var candidate = await jobRepository.GetNextRunnableAsync(staleBefore, cancellationToken);
        if (candidate is null) return false;

        if (!await jobRepository.TryClaimAsync(
                candidate.Id,
                now,
                staleBefore,
                cancellationToken))
            return true;

        var job = await jobRepository.GetByIdAsync(candidate.Id, cancellationToken)
            ?? throw new InvalidOperationException("Claimed plan generation job disappeared.");
        logger.LogInformation(
            "Plan generation worker started event {EventId}, generation job {GenerationJobId}, request {RequestId}, attempt {AttemptCount}",
            job.EventId,
            job.Id,
            job.RequestId,
            job.AttemptCount);

        try
        {
            var started = Stopwatch.GetTimestamp();
            await generationService.GeneratePlanAsync(
                job.EventId,
                job.RequestedById,
                job.Id,
                cancellationToken,
                job.Regenerate);
            logger.LogInformation(
                "Plan generation completed for event {EventId}, generation job {GenerationJobId}, request {RequestId}, elapsed {ElapsedMilliseconds}ms",
                job.EventId,
                job.Id,
                job.RequestId,
                (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Plan generation worker cancelled event {EventId}, generation job {GenerationJobId}, request {RequestId}; the persisted job will be resumed after its lease expires",
                job.EventId,
                job.Id,
                job.RequestId);
            throw;
        }
        catch (Exception exception)
        {
            await jobRepository.MarkFailedIfProcessingAsync(
                job.Id,
                "Plan generation failed. Please retry.",
                DateTime.UtcNow,
                cancellationToken);

            if (exception is TaskCanceledException ||
                exception is HttpRequestException { StatusCode: HttpStatusCode.GatewayTimeout })
            {
                logger.LogWarning(
                    "Plan generation timed out for event {EventId}, generation job {GenerationJobId}, request {RequestId}",
                    job.EventId,
                    job.Id,
                    job.RequestId);
            }
            logger.LogError(
                exception,
                "Plan generation failed event {EventId}, generation job {GenerationJobId}, request {RequestId}, failure type {FailureType}",
                job.EventId,
                job.Id,
                job.RequestId,
                exception.GetType().Name);
            return true;
        }
    }

    private void EnsureAuthorized(Event eventEntity, Guid userId)
    {
        if (currentUser.IsAdmin || userId != eventEntity.OwnerId)
            throw new UnauthorizedAccessException("You are not authorized to plan this event.");
    }
}
