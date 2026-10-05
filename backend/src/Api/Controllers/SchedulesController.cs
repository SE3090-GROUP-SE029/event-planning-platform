using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Diagnostics;
using Application.DTOs.Scheduling;
using Application.Services.Scheduling;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SchedulesController : ControllerBase
{
    private readonly ScheduleService _scheduleService;
    private readonly ILogger<SchedulesController> _logger;

    public SchedulesController(
        ScheduleService scheduleService,
        ILogger<SchedulesController> logger)
    {
        _scheduleService = scheduleService;
        _logger = logger;
    }

    [HttpGet("event/{eventId:guid}")]
    [Authorize(Policy = "EventPlannerOnly")]
    public async Task<IActionResult> GetSchedule(Guid eventId, CancellationToken ct)
    {
        try
        {
            var schedule = await _scheduleService.GetOrCreateScheduleForPlannerAsync(eventId, GetCurrentUserId(), ct);
            return Ok(schedule);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("vendor/me")]
    [Authorize(Policy = "VendorOnly")]
    public async Task<IActionResult> GetMyVendorActivities(CancellationToken ct)
    {
        try
        {
            var activities = await _scheduleService.GetAssignedActivitiesForVendorAsync(GetCurrentUserId(), ct);
            return Ok(activities);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{scheduleId:guid}/activities")]
    [Authorize(Policy = "EventPlannerOnly")]
    public async Task<IActionResult> AddActivity(
        Guid scheduleId,
        [FromBody] CreateActivityRequest request,
        CancellationToken ct)
    {
        if (request.EndTime <= request.StartTime)
        {
            return BadRequest(new { message = "EndTime must be strictly after StartTime." });
        }

        try
        {
            var activity = await _scheduleService.AddActivityForPlannerAsync(
                scheduleId,
                GetCurrentUserId(),
                request.Title,
                request.Description,
                request.StartTime,
                request.EndTime,
                request.AssignedVendorId,
                ct);

            return Created($"/api/Schedules/{scheduleId}/activities/{activity.Id}", activity);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("activities/{activityId:guid}")]
    [Authorize(Policy = "EventPlannerOnly")]
    public async Task<IActionResult> UpdateActivity(
        Guid activityId,
        [FromBody] UpdateActivityRequest request,
        CancellationToken ct)
    {
        try
        {
            var activity = await _scheduleService.UpdateActivityForPlannerAsync(
                activityId,
                GetCurrentUserId(),
                request.Title,
                request.Description,
                request.StartTime,
                request.EndTime,
                request.AssignedVendorId,
                ct);

            if (activity == null)
            {
                return NotFound(new { message = "Activity not found." });
            }

            return Ok(activity);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("activities/{activityId:guid}")]
    [Authorize(Policy = "EventPlannerOnly")]
    public async Task<IActionResult> DeleteActivity(Guid activityId, CancellationToken ct)
    {
        try
        {
            var deleted = await _scheduleService.DeleteActivityForPlannerAsync(
                activityId,
                GetCurrentUserId(),
                ct);

            if (deleted == null)
            {
                return NotFound(new { message = "Activity not found." });
            }

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPatch("activities/{activityId:guid}/status")]
    [Authorize(Roles = "EVENT_PLANNER,VENDOR")]
    public async Task<IActionResult> UpdateActivityStatus(
        Guid activityId,
        [FromBody] UpdateActivityStatusRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = GetCurrentUserId();
            var updatedActivity = User.IsInRole("VENDOR")
                ? await _scheduleService.UpdateActivityStatusForVendorAsync(activityId, userId, request.Status, ct)
                : await _scheduleService.UpdateActivityStatusForPlannerAsync(activityId, userId, request.Status, ct);

            if (updatedActivity == null)
            {
                return NotFound(new { message = "Activity not found." });
            }

            return Ok(updatedActivity);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("{scheduleId:guid}/conflicts")]
    [Authorize(Policy = "EventPlannerOnly")]
    public async Task<IActionResult> GetConflicts(Guid scheduleId, CancellationToken ct)
    {
        try
        {
            var conflicts = await _scheduleService.GetConflictsForPlannerAsync(
                scheduleId,
                GetCurrentUserId(),
                ct);
            return Ok(conflicts);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("{scheduleId:guid}/generate-ai")]
    [Authorize(Policy = "EventPlannerOnly")]
    public async Task<IActionResult> GenerateAiSchedule(Guid scheduleId, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestStartedAt = DateTimeOffset.UtcNow;
        int? statusCode = null;
        string? timeoutSource = null;
        string? cancellationSource = null;
        string? exceptionType = null;
        _logger.LogInformation(
            "AI schedule API request started for schedule {ScheduleId} at {RequestStartedAt}",
            scheduleId,
            requestStartedAt);

        try
        {
            var updatedSchedule = await _scheduleService.GenerateScheduleWithAiForPlannerAsync(
                scheduleId,
                GetCurrentUserId(),
                ct);
            statusCode = StatusCodes.Status200OK;
            _logger.LogInformation(
                "AI schedule API request succeeded for schedule {ScheduleId}: status {StatusCode}, {ActivityCount} activities and {ConflictCount} conflicts",
                scheduleId,
                statusCode,
                updatedSchedule.Activities.Count,
                updatedSchedule.Conflicts.Count);
            return Ok(updatedSchedule);
        }
        catch (TimeoutException ex)
        {
            statusCode = StatusCodes.Status504GatewayTimeout;
            timeoutSource = ex.InnerException is OperationCanceledException
                ? "Backend"
                : "AI Service";
            exceptionType = ex.GetType().FullName;
            _logger.LogError(
                ex,
                "AI schedule API request timed out for schedule {ScheduleId} after {ElapsedMilliseconds} ms; timeout source {TimeoutSource}, status {StatusCode}, exception type {ExceptionType}",
                scheduleId,
                stopwatch.ElapsedMilliseconds,
                timeoutSource,
                statusCode,
                exceptionType);
            return StatusCode(statusCode.Value, new
            {
                message = "AI schedule generation exceeded the maximum allowed processing time."
            });
        }
        catch (OperationCanceledException ex)
        {
            cancellationSource = ct.IsCancellationRequested
                ? "RequestAborted"
                : "UpstreamCancellation";
            exceptionType = ex.GetType().FullName;
            _logger.LogWarning(
                ex,
                "AI schedule API request cancelled for schedule {ScheduleId}; cancellation source {CancellationSource}, exception type {ExceptionType}",
                scheduleId,
                cancellationSource,
                exceptionType);
            throw;
        }
        catch (KeyNotFoundException ex)
        {
            statusCode = StatusCodes.Status404NotFound;
            exceptionType = ex.GetType().FullName;
            _logger.LogWarning(
                ex,
                "AI schedule API request failed for schedule {ScheduleId}: status {StatusCode}, exception type {ExceptionType}",
                scheduleId,
                statusCode,
                exceptionType);
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            statusCode = StatusCodes.Status403Forbidden;
            exceptionType = ex.GetType().FullName;
            _logger.LogWarning(
                ex,
                "AI schedule API request failed for schedule {ScheduleId}: status {StatusCode}, exception type {ExceptionType}",
                scheduleId,
                statusCode,
                exceptionType);
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            statusCode = StatusCodes.Status400BadRequest;
            exceptionType = ex.GetType().FullName;
            _logger.LogWarning(
                ex,
                "AI schedule API request failed for schedule {ScheduleId}: status {StatusCode}, exception type {ExceptionType}",
                scheduleId,
                statusCode,
                exceptionType);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            statusCode = StatusCodes.Status500InternalServerError;
            exceptionType = ex.GetType().FullName;
            _logger.LogError(
                ex,
                "AI schedule API request failed for schedule {ScheduleId}: status {StatusCode}, exception type {ExceptionType}",
                scheduleId,
                statusCode,
                exceptionType);
            throw;
        }
        finally
        {
            _logger.LogInformation(
                "AI schedule API request completed for schedule {ScheduleId} at {RequestCompletedAt}; total duration {ElapsedMilliseconds} ms; status {StatusCode}; timeout source {TimeoutSource}; cancellation source {CancellationSource}; exception type {ExceptionType}",
                scheduleId,
                DateTimeOffset.UtcNow,
                stopwatch.ElapsedMilliseconds,
                statusCode?.ToString() ?? "not returned",
                timeoutSource ?? "None",
                cancellationSource ?? "None",
                exceptionType ?? "None");
        }
    }

    private Guid GetCurrentUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!Guid.TryParse(raw, out var userId))
        {
            throw new UnauthorizedAccessException("The access token does not contain a user id.");
        }

        return userId;
    }
}

public record UpdateActivityStatusRequest(ActivityStatus Status);
