using System.Data.Common;
using Application.Dtos.Admin;
using Application.Services.Admin;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminController(
    IAdminReadService service,
    AppDbContext db) : ControllerBase
{
    [HttpGet("analytics")]
    public Task<AdminAnalyticsResponse> Analytics(CancellationToken cancellationToken) =>
        service.GetAnalyticsAsync(cancellationToken);

    [HttpGet("users")]
    public async Task<ActionResult<AdminUserListResponse>> Users([FromQuery] AdminUserQuery query, CancellationToken cancellationToken)
    {
        try { return Ok(await service.ListUsersAsync(query, cancellationToken)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("plans")]
    public async Task<ActionResult<AdminPlanListResponse>> Plans([FromQuery] AdminPlanQuery query, CancellationToken cancellationToken)
    {
        try { return Ok(await service.ListPlansAsync(query, cancellationToken)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("plans/{id:guid}")]
    public async Task<ActionResult<AdminPlanResponse>> Plan(Guid id, CancellationToken cancellationToken)
    {
        try { return Ok(await service.GetPlanAsync(id, cancellationToken)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("health")]
    public async Task<ActionResult<AdminHealthResponse>> Health(CancellationToken cancellationToken)
    {
        var checkedAt = DateTime.UtcNow;
        try
        {
            if (!await db.Database.CanConnectAsync(cancellationToken))
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new AdminHealthResponse { Api = "healthy", Database = "unreachable", CheckedAt = checkedAt });
            return Ok(new AdminHealthResponse { Api = "healthy", Database = "healthy", CheckedAt = checkedAt });
        }
        catch (Exception ex) when (ex is DbException or TimeoutException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new AdminHealthResponse { Api = "healthy", Database = "unreachable", CheckedAt = checkedAt });
        }
    }
}
