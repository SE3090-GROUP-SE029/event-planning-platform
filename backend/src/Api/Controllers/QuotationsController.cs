using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Dtos.Quotations;
using Application.Services.Quotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/quotations")]
[Authorize]
public class QuotationsController : ControllerBase
{
    private readonly IQuotationService _quotations;

    public QuotationsController(IQuotationService quotations)
    {
        _quotations = quotations;
    }

    [HttpPost]
    [Authorize(Policy = "EventPlannerOnly")]
    public async Task<ActionResult<QuotationResponse>> Create(CreateQuotationRequest request)
    {
        try
        {
            var result = await _quotations.CreateAsync(GetCurrentUserId(), request);
            return Created($"/api/quotations/{result.Id}", result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("mine")]
    [Authorize(Policy = "EventPlannerOnly")]
    public async Task<ActionResult<IReadOnlyList<QuotationResponse>>> ListMine()
    {
        return Ok(await _quotations.ListMineAsync(GetCurrentUserId()));
    }

    [HttpGet("vendor")]
    [Authorize(Policy = "VendorOnly")]
    public async Task<ActionResult<IReadOnlyList<QuotationResponse>>> ListForVendor()
    {
        try
        {
            return Ok(await _quotations.ListForVendorAsync(GetCurrentUserId()));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QuotationResponse>> GetById(Guid id)
    {
        try
        {
            var result = await _quotations.GetByIdAsync(
                id,
                GetCurrentUserId(),
                User.IsInRole("VENDOR"));
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}/respond")]
    [Authorize(Policy = "VendorOnly")]
    public async Task<ActionResult<QuotationResponse>> Respond(Guid id, RespondToQuotationRequest request)
    {
        try
        {
            return Ok(await _quotations.RespondAsync(GetCurrentUserId(), id, request));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
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
