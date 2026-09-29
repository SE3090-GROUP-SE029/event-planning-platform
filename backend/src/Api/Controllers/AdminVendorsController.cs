using Application.Dtos.Vendors;
using Application.Services.Vendors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/admin/vendors")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminVendorsController : ControllerBase
{
    private readonly IAdminVendorService _service;

    public AdminVendorsController(IAdminVendorService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<AdminVendorListResponse>> List([FromQuery] AdminVendorQuery query)
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
    public async Task<ActionResult<AdminVendorResponse>> GetById(Guid id)
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

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<AdminVendorResponse>> Approve(Guid id)
    {
        try
        {
            return Ok(await _service.ApproveAsync(id));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/suspend")]
    public async Task<ActionResult<AdminVendorResponse>> Suspend(Guid id)
    {
        try
        {
            return Ok(await _service.SuspendAsync(id));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<ActionResult<AdminVendorResponse>> Restore(Guid id)
    {
        try
        {
            return Ok(await _service.RestoreAsync(id));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
