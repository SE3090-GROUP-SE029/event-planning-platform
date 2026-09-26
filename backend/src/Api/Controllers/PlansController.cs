using System.Security.Claims;
using System.Net;
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
    IPlanGenerationService generationService,
    IPlanDecisionService decisionService,
    IEventPlanDraftRepository planRepository,
    IEventRepository eventRepository) : ControllerBase
{
    [HttpPost("events/{eventId:guid}/plans/generate")]
    public async Task<ActionResult<ApiResponse<EventPlanDraft>>> Generate(
        Guid eventId,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] GeneratePlanRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is not null)
        {
            if (request.EventId != eventId)
                return BadRequest(ApiResponse<EventPlanDraft>.Error("EventId does not match route."));
            var validationErrors = request.GetValidationErrors()
                .Select(error => error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message));
            var error = string.Join("; ", validationErrors);
            if (!string.IsNullOrEmpty(error))
                return BadRequest(ApiResponse<EventPlanDraft>.Error(error));
        }

        try
        {
            var plan = await generationService.GeneratePlanAsync(
                eventId,
                cancellationToken,
                request?.Regenerate ?? false);
            return Accepted(ApiResponse<EventPlanDraft>.Ok(plan, "Plan generation completed."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<EventPlanDraft>.Error(ex.Message, 404)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse<EventPlanDraft>.Error(ex.Message)); }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(
                StatusCodes.Status504GatewayTimeout,
                ApiResponse<EventPlanDraft>.Error("Plan generation timed out. Please try again.", 504));
        }
        catch (HttpRequestException ex)
        {
            var statusCode = ex.StatusCode switch
            {
                HttpStatusCode.BadGateway => StatusCodes.Status502BadGateway,
                HttpStatusCode.GatewayTimeout => StatusCodes.Status504GatewayTimeout,
                _ => StatusCodes.Status503ServiceUnavailable
            };
            return StatusCode(
                statusCode,
                ApiResponse<EventPlanDraft>.Error(
                    "The AI planning service is temporarily unavailable. Please try again.",
                    statusCode));
        }
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

        var plans = await planRepository.ListAsync(eventId, status, version, cancellationToken);
        return Ok(ApiListResponse<EventPlanDraft>.Ok(
            plans,
            new ApiPagination { Page = 1, PageSize = plans.Count, Total = plans.Count, HasNextPage = false }));
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
            return Ok(ApiResponse<EventPlanDraft>.Ok(plan));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<EventPlanDraft>.Error(ex.Message, 404)); }
    }

    [HttpPost("plans/{planId:guid}/approve")]
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
