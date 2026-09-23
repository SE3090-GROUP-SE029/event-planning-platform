using System.Security.Claims;
using Application.Common.Interfaces;
using Application.Dtos.Common;
using Application.Dtos.Plans;
using Application.Services.Planning;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class PlansController(
    IPlanGenerationService generationService,
    IPlanDecisionService decisionService,
    IEventPlanDraftRepository planRepository) : ControllerBase
{
    [HttpPost("events/{eventId:guid}/plans/generate")]
    [Authorize(Policy = "EventPlannerOnly")]
    public async Task<ActionResult<ApiResponse<EventPlanDraft>>> Generate(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        try
        {
            var plan = await generationService.GeneratePlanAsync(eventId, cancellationToken);
            return Accepted(ApiResponse<EventPlanDraft>.Ok(plan, "Plan generation completed."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<EventPlanDraft>.Error(ex.Message, 404)); }
        catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse<EventPlanDraft>.Error(ex.Message)); }
        catch (HttpRequestException ex) { return StatusCode(503, ApiResponse<EventPlanDraft>.Error(ex.Message, 503)); }
    }

    [HttpGet("events/{eventId:guid}/plans")]
    [Authorize(Policy = "EventPlannerOnly")]
    public async Task<ActionResult<ApiListResponse<EventPlanDraft>>> List(
        Guid eventId,
        [FromQuery] PlanStatus? status,
        [FromQuery] int? version,
        CancellationToken cancellationToken)
    {
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
