using Application.Dtos.Events;
using Application.Services.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/admin/events")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminEventsController : ControllerBase
{
    private readonly IAdminEventService _service;

    public AdminEventsController(IAdminEventService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<AdminEventListResponse>> List([FromQuery] AdminEventQuery query)
    {
        try
        {
            return Ok(await _service.ListAsync(query));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminEventResponse>> GetById(Guid id)
    {
        try
        {
            return Ok(await _service.GetByIdAsync(id));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
