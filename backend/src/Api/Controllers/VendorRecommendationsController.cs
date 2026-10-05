using System.Security.Claims;
using Application.Dtos.Common;
using Application.Dtos.Vendors;
using Application.Services.Vendors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Api.Controllers;

[ApiController]
[Route("api")]
[Authorize(Policy = "EventPlannerOnly")]
public sealed class VendorRecommendationsController(
    IVendorRecommendationService recommendationService,
    ILogger<VendorRecommendationsController> logger) : ControllerBase
{
    [HttpPost("events/{eventId:guid}/vendor-recommendations")]
    public async Task<ActionResult<ApiResponse<VendorRecommendationRunResponse>>> Generate(
        Guid eventId,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] GenerateVendorRecommendationsRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = RequireUserId();
            var result = await recommendationService.GenerateAsync(
                eventId,
                userId,
                request,
                cancellationToken);
            logger.LogInformation(
                "Vendor recommendation request accepted event {EventId}, run {RunId}, request {RequestId}, status {Status}",
                eventId,
                result.Id,
                HttpContext.TraceIdentifier,
                result.Status);
            var response = ApiResponse<VendorRecommendationRunResponse>.Ok(
                result,
                result.Status == Domain.Entities.VendorRecommendationRun.CompletedStatus
                    ? "Returned the latest completed recommendations."
                : "Vendor recommendation generation queued.");
            return result.Status == Domain.Entities.VendorRecommendationRun.CompletedStatus
                ? Ok(response)
                : AcceptedAtAction(
                nameof(GetRun),
                new { eventId, runId = result.Id },
                response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<VendorRecommendationRunResponse>.Error(ex.Message, 404));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<VendorRecommendationRunResponse>.Error(ex.Message));
        }
    }

    [HttpGet("events/{eventId:guid}/vendor-recommendations")]
    public async Task<ActionResult<ApiResponse<VendorRecommendationRunResponse>>> GetLatest(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = RequireUserId();
            var result = await recommendationService.GetLatestAsync(eventId, userId, cancellationToken);
            if (result is null)
            {
                logger.LogWarning(
                    "Vendor recommendation retrieval failed event {EventId}, request {RequestId}: no run found",
                    eventId,
                    HttpContext.TraceIdentifier);
                return NotFound(ApiResponse<VendorRecommendationRunResponse>.Error(
                    "No vendor recommendations found for this event.",
                    404));
            }

            logger.LogInformation(
                "Vendor recommendation retrieval succeeded event {EventId}, run {RunId}, request {RequestId}, status {Status}",
                eventId,
                result.Id,
                HttpContext.TraceIdentifier,
                result.Status);
            return Ok(ApiResponse<VendorRecommendationRunResponse>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(
                ex,
                "Vendor recommendation retrieval failed event {EventId}, request {RequestId}",
                eventId,
                HttpContext.TraceIdentifier);
            return NotFound(ApiResponse<VendorRecommendationRunResponse>.Error(ex.Message, 404));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("events/{eventId:guid}/vendor-recommendations/{runId:guid}")]
    public async Task<ActionResult<ApiResponse<VendorRecommendationRunResponse>>> GetRun(
        Guid eventId,
        Guid runId,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = RequireUserId();
            var result = await recommendationService.GetRunAsync(
                eventId,
                runId,
                userId,
                cancellationToken);
            if (result is null)
            {
                logger.LogWarning(
                    "Vendor recommendation retrieval failed event {EventId}, run {RunId}, request {RequestId}: run not found",
                    eventId,
                    runId,
                    HttpContext.TraceIdentifier);
                return NotFound(ApiResponse<VendorRecommendationRunResponse>.Error(
                    "Vendor recommendation run not found.",
                    404));
            }

            logger.LogInformation(
                "Vendor recommendation retrieval succeeded event {EventId}, run {RunId}, request {RequestId}, status {Status}",
                eventId,
                runId,
                HttpContext.TraceIdentifier,
                result.Status);
            return Ok(ApiResponse<VendorRecommendationRunResponse>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(
                ex,
                "Vendor recommendation retrieval failed event {EventId}, run {RunId}, request {RequestId}",
                eventId,
                runId,
                HttpContext.TraceIdentifier);
            return NotFound(ApiResponse<VendorRecommendationRunResponse>.Error(ex.Message, 404));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private Guid RequireUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(raw, out var userId))
            throw new UnauthorizedAccessException();
        return userId;
    }
}
