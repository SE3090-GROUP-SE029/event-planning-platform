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
        if (request.EndTime <= request.StartTime)
        {
            return BadRequest(new { message = "EndTime must be strictly after StartTime." });
        }

        var activity = await _scheduleService.AddActivityAsync(
            scheduleId,
            request.Title,
            request.Description,
            request.StartTime,
            request.EndTime,
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

    [HttpPost("{scheduleId:guid}/generate-ai")]
    [Authorize]
    public async Task<IActionResult> GenerateAiSchedule(Guid scheduleId, CancellationToken ct)
    {
        try
        {
            var updatedSchedule = await _scheduleService.GenerateScheduleWithAiAsync(scheduleId, ct);
            return Ok(updatedSchedule);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

public record UpdateActivityStatusRequest(ActivityStatus Status);