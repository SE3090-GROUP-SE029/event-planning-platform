using Application.Dtos.Admin;
using Application.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/admin/schedules")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminSchedulesController : ControllerBase
{
    private readonly IAdminScheduleService _service;

    public AdminSchedulesController(IAdminScheduleService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<AdminScheduleListResponse>> List(
        [FromQuery] AdminScheduleQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.ListAsync(query, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{scheduleId:guid}")]
    public async Task<ActionResult<AdminScheduleDetailResponse>> GetById(
        Guid scheduleId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.GetByIdAsync(scheduleId, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
