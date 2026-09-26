using Application.DTOs.Scheduling;
using Application.Services.Scheduling;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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
}