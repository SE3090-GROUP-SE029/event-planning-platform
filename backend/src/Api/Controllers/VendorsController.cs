using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Dtos.Vendors;
using Application.Services.Vendors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "VendorOnly")]
public class VendorsController : ControllerBase
{
    private readonly IVendorService _vendorService;

    public VendorsController(IVendorService vendorService)
    {
        _vendorService = vendorService;
    }

    [HttpPost]
    public async Task<ActionResult<VendorProfileResponse>> Create(CreateVendorProfileRequest request)
    {
        try
        {
            var result = await _vendorService.CreateProfileAsync(GetCurrentUserId(), request);
            return CreatedAtAction(nameof(GetMe), value: result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("me")]
    public async Task<ActionResult<VendorProfileResponse>> GetMe()
    {
        try
        {
            var result = await _vendorService.GetMyProfileAsync(GetCurrentUserId());
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("me")]
    public async Task<ActionResult<VendorProfileResponse>> UpdateMe(UpdateVendorProfileRequest request)
    {
        try
        {
            var result = await _vendorService.UpdateMyProfileAsync(GetCurrentUserId(), request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
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
