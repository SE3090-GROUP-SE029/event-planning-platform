using Application.Common;
using Application.DTOs.Scheduling;
using Application.Services.Scheduling;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "EventPlannerOnly")]
public class SchedulesController : ControllerBase
{
    private readonly ScheduleService _scheduleService;

    public SchedulesController(ScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    [HttpGet("event/{eventId:guid}")]
    public async Task<IActionResult> GetSchedule(Guid eventId, CancellationToken ct)
    {
        var schedule = await _scheduleService.GetOrCreateScheduleAsync(eventId, ct);
        return Ok(schedule);
    }

    [HttpPost("{scheduleId:guid}/activities")]
    public async Task<IActionResult> AddActivity(
        Guid scheduleId, 
        [FromBody] CreateActivityRequest request, 
        CancellationToken ct)
    {
        var startTime = UtcDateTime.Normalize(request.StartTime);
        var endTime = UtcDateTime.Normalize(request.EndTime);
        if (endTime <= startTime)
        {
            return BadRequest(new { message = "EndTime must be strictly after StartTime." });
        }

        var activity = await _scheduleService.AddActivityAsync(
            scheduleId,
            request.Title,
            request.Description,
            startTime,
            endTime,
            request.AssignedVendorId,
            ct);

        return CreatedAtAction(nameof(GetSchedule), new { eventId = activity.ScheduleId }, activity);
    }

    [HttpPatch("activities/{activityId:guid}/status")]
    public async Task<IActionResult> UpdateActivityStatus(
        Guid activityId, 
        [FromBody] UpdateActivityStatusRequest request, 
        CancellationToken ct)
    {
        var updatedActivity = await _scheduleService.UpdateActivityStatusAsync(activityId, request.Status, ct);
        if (updatedActivity == null)
        {
            return NotFound(new { message = "Activity not found." });
        }

        return Ok(updatedActivity);
    }
}

public record UpdateActivityStatusRequest(ActivityStatus Status);