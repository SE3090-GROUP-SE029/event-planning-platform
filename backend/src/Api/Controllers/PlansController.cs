using System.Security.Claims;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Dtos.Common;
using Application.Dtos.Plans;
using Application.Services.Planning;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class PlansController(
    IPlanGenerationJobService generationJobService,
    IPlanGenerationJobRepository generationJobRepository,
    IPlanDecisionService decisionService,
    IEventPlanDraftRepository planRepository,
    IEventRepository eventRepository,
    ILogger<PlansController> logger) : ControllerBase
{
    [HttpPost("events/{eventId:guid}/plans/generate")]
    [Authorize(Policy = "EventPlannerOnly")]
    public async Task<ActionResult<ApiResponse<PlanGenerationJobResponse>>> Generate(
        Guid eventId,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] GeneratePlanRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is not null)
        {
            if (request.EventId != eventId)
                return BadRequest(ApiResponse<PlanGenerationJobResponse>.Error("EventId does not match route."));
            var validationErrors = request.GetValidationErrors()
                .Select(error => error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message));
            var error = string.Join("; ", validationErrors);
            if (!string.IsNullOrEmpty(error))
                return BadRequest(ApiResponse<PlanGenerationJobResponse>.Error(error));
        }

        try
        {
            var job = await generationJobService.EnqueueAsync(
                eventId,
                request?.Regenerate ?? false,
                HttpContext.TraceIdentifier,
                cancellationToken);
            return AcceptedAtAction(
                nameof(GetGenerationJob),
                new { eventId, jobId = job.Id },
                ApiResponse<PlanGenerationJobResponse>.Ok(
                    PlanGenerationJobResponse.From(job),
                    "Plan generation queued."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<PlanGenerationJobResponse>.Error(ex.Message, 404)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse<PlanGenerationJobResponse>.Error(ex.Message)); }
    }

    [HttpGet("events/{eventId:guid}/plans/generation/latest")]
    public async Task<ActionResult<ApiResponse<PlanGenerationJobResponse?>>> GetLatestGeneration(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(eventId);
        if (eventEntity is null)
            return NotFound(new { message = "Event not found." });
        if (!User.IsInRole("ADMIN") && !IsCurrentUser(eventEntity.OwnerId))
            return Forbid();

        var job = await generationJobRepository.GetLatestForEventAsync(eventId, cancellationToken);
        logger.LogInformation(
            "Plan generation status retrieved for event {EventId}, generation job {GenerationJobId}, request {RequestId}",
            eventId,
            job?.Id,
            HttpContext.TraceIdentifier);
        return Ok(ApiResponse<PlanGenerationJobResponse?>.Ok(
            job is null ? null : PlanGenerationJobResponse.From(job)));
    }

    [HttpGet("events/{eventId:guid}/plans/generation/{jobId:guid}")]
    public async Task<ActionResult<ApiResponse<PlanGenerationJobResponse>>> GetGenerationJob(
        Guid eventId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(eventId);
        if (eventEntity is null)
        {
            logger.LogWarning(
                "Plan generation status retrieval failed for missing event {EventId}, request {RequestId}",
                eventId,
                HttpContext.TraceIdentifier);
            return NotFound(new { message = "Event not found." });
        }
        if (!User.IsInRole("ADMIN") && !IsCurrentUser(eventEntity.OwnerId))
            return Forbid();

        var job = await generationJobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job is null || job.EventId != eventId)
        {
            logger.LogWarning(
                "Plan generation status retrieval failed for event {EventId}, generation job {GenerationJobId}, request {RequestId}",
                eventId,
                jobId,
                HttpContext.TraceIdentifier);
            return NotFound(ApiResponse<PlanGenerationJobResponse>.Error("Plan generation job not found.", 404));
        }

        logger.LogInformation(
            "Plan generation status retrieved for event {EventId}, generation job {GenerationJobId}, request {RequestId}, status {Status}",
            eventId,
            jobId,
            job.RequestId,
            job.Status);
        return Ok(ApiResponse<PlanGenerationJobResponse>.Ok(PlanGenerationJobResponse.From(job)));
    }

    [HttpGet("events/{eventId:guid}/plans")]
    public async Task<ActionResult<ApiListResponse<EventPlanDraft>>> List(
        Guid eventId,
        [FromQuery] PlanStatus? status,
        [FromQuery] int? version,
        CancellationToken cancellationToken)
    {
        var eventEntity = await eventRepository.GetByIdAsync(eventId);
        if (eventEntity is null)
            return NotFound(new { message = "Event not found." });
        if (!User.IsInRole("ADMIN") && !IsCurrentUser(eventEntity.OwnerId))
            return Forbid();

        try
        {
            var plans = await planRepository.ListAsync(eventId, status, version, cancellationToken);
            logger.LogInformation(
                "Plan retrieval succeeded for event {EventId}, request {RequestId}, plan count {PlanCount}",
                eventId,
                HttpContext.TraceIdentifier,
                plans.Count);
            return Ok(ApiListResponse<EventPlanDraft>.Ok(
                plans,
                new ApiPagination { Page = 1, PageSize = plans.Count, Total = plans.Count, HasNextPage = false }));
        }
        catch (PersistedJsonDeserializationException ex)
        {
            logger.LogError(
                ex,
                "Plan retrieval failed for event {EventId}, request {RequestId}; persisted {DataType} JSON is invalid. Raw JSON: {RawJson}",
                eventId,
                HttpContext.TraceIdentifier,
                ex.DataType,
                ex.RawJson);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<EventPlanDraft>.Error(
                    "Saved plan data is corrupted and could not be retrieved.",
                    StatusCodes.Status500InternalServerError));
        }
    }

    [HttpGet("plans/{planId:guid}")]
    public async Task<ActionResult<ApiResponse<EventPlanDraft>>> Get(
        Guid planId,
        CancellationToken cancellationToken)
    {
        try
        {
            var plan = await planRepository.GetByIdAsync(planId, cancellationToken)
                ?? throw new KeyNotFoundException("Plan not found.");
            if (!User.IsInRole("ADMIN") && !IsCurrentUser(plan.Event.OwnerId))
                return Forbid();
            logger.LogInformation(
                "Plan retrieval succeeded for event {EventId}, plan {PlanId}, request {RequestId}",
                plan.EventId,
                planId,
                HttpContext.TraceIdentifier);
            return Ok(ApiResponse<EventPlanDraft>.Ok(plan));
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(
                "Plan retrieval failed for plan {PlanId}, request {RequestId}: not found",
                planId,
                HttpContext.TraceIdentifier);
            return NotFound(ApiResponse<EventPlanDraft>.Error(ex.Message, 404));
        }
        catch (PersistedJsonDeserializationException ex)
        {
            logger.LogError(
                ex,
                "Plan retrieval failed for plan {PlanId}, request {RequestId}; persisted {DataType} JSON is invalid. Raw JSON: {RawJson}",
                planId,
                HttpContext.TraceIdentifier,
                ex.DataType,
                ex.RawJson);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<EventPlanDraft>.Error(
                    "Saved plan data is corrupted and could not be retrieved.",
                    StatusCodes.Status500InternalServerError));
        }
    }

    [HttpPost("plans/{planId:guid}/approve")]
    [Authorize(Policy = "EventPlannerOnly")]
    public async Task<ActionResult<ApiResponse<EventPlanDraft>>> Approve(
        Guid planId,
        [FromBody] ApprovePlanRequest request,
        CancellationToken cancellationToken)
    {
        if (request.PlanId != Guid.Empty && request.PlanId != planId)
            return BadRequest(ApiResponse<EventPlanDraft>.Error("PlanId does not match route."));
        try
        {
            var plan = await decisionService.ApprovePlanAsync(planId, request.ApproverNotes, cancellationToken);
            return Ok(ApiResponse<EventPlanDraft>.Ok(plan, "Plan approved."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<EventPlanDraft>.Error(ex.Message, 404)); }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<EventPlanDraft>.Error(ex.Message, 409)); }
    }

    [HttpPost("plans/{planId:guid}/reject")]
    [Authorize(Policy = "EventPlannerOnly")]
    public async Task<ActionResult<ApiResponse<EventPlanDraft>>> Reject(
        Guid planId,
        [FromBody] RejectPlanRequest request,
        CancellationToken cancellationToken)
    {
        if (request.PlanId != Guid.Empty && request.PlanId != planId)
            return BadRequest(ApiResponse<EventPlanDraft>.Error("PlanId does not match route."));
        try
        {
            var plan = await decisionService.RejectPlanAsync(
                planId, request.Remarks ?? string.Empty, request.Severity, cancellationToken);
            return Ok(ApiResponse<EventPlanDraft>.Ok(plan, "Plan rejected."));
        }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<EventPlanDraft>.Error(ex.Message)); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<EventPlanDraft>.Error(ex.Message, 404)); }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<EventPlanDraft>.Error(ex.Message, 409)); }
    }

    private bool IsCurrentUser(Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var current) &&
        current == userId;
}
