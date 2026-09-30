using System.Net;
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
    IVendorRecommendationService recommendationService) : ControllerBase
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
            return Ok(ApiResponse<VendorRecommendationRunResponse>.Ok(
                result,
                result.FromCache
                    ? "Returned the last saved recommendations because the AI service is unavailable."
                    : "Vendor recommendations generated."));
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
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(
                StatusCodes.Status504GatewayTimeout,
                ApiResponse<VendorRecommendationRunResponse>.Error(
                    "Vendor recommendation timed out. Please try again.",
                    504));
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
                ApiResponse<VendorRecommendationRunResponse>.Error(
                    "The AI recommendation service is temporarily unavailable. Please try again.",
                    statusCode));
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
                return NotFound(ApiResponse<VendorRecommendationRunResponse>.Error(
                    "No vendor recommendations found for this event.",
                    404));
            }

            return Ok(ApiResponse<VendorRecommendationRunResponse>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
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
