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
    private readonly IVendorOfferingService _offeringService;
    private readonly IVendorGalleryService _galleryService;
    private readonly IVendorAvailabilityService _availabilityService;

    public VendorsController(
        IVendorService vendorService,
        IVendorOfferingService offeringService,
        IVendorGalleryService galleryService,
        IVendorAvailabilityService availabilityService)
    {
        _vendorService = vendorService;
        _offeringService = offeringService;
        _galleryService = galleryService;
        _availabilityService = availabilityService;
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

    [HttpPost("me/profile-image")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 3 * 1024 * 1024)]
    public async Task<ActionResult<VendorProfileResponse>> UploadProfileImage(IFormFile? file)
    {
        try
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest(new { message = "An image file is required." });
            }

            await using var stream = file.OpenReadStream();
            var result = await _vendorService.UpdateProfileImageAsync(
                GetCurrentUserId(),
                stream,
                file.ContentType ?? "application/octet-stream",
                file.Length);
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

    [HttpGet("me/images")]
    public async Task<ActionResult<IReadOnlyList<VendorGalleryImageResponse>>> ListMyImages()
    {
        try
        {
            var result = await _galleryService.ListMineAsync(GetCurrentUserId());
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("me/images")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 3 * 1024 * 1024)]
    public async Task<ActionResult<VendorGalleryImageResponse>> UploadGalleryImage(IFormFile? file)
    {
        try
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest(new { message = "An image file is required." });
            }

            await using var stream = file.OpenReadStream();
            var result = await _galleryService.UploadAsync(
                GetCurrentUserId(),
                stream,
                file.ContentType ?? "application/octet-stream",
                file.Length);
            return CreatedAtAction(nameof(ListMyImages), value: result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("me/images/{id:guid}")]
    public async Task<IActionResult> DeleteGalleryImage(Guid id)
    {
        try
        {
            await _galleryService.DeleteAsync(GetCurrentUserId(), id);
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

    [HttpGet("me/services")]
    public async Task<ActionResult<IReadOnlyList<VendorOfferingResponse>>> ListMyServices()
    {
        try
        {
            var result = await _offeringService.ListMineAsync(GetCurrentUserId());
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("me/services")]
    public async Task<ActionResult<VendorOfferingResponse>> CreateService(CreateVendorOfferingRequest request)
    {
        try
        {
            var result = await _offeringService.CreateAsync(GetCurrentUserId(), request);
            return CreatedAtAction(nameof(ListMyServices), value: result);
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

    [HttpPut("me/services/{id:guid}")]
    public async Task<ActionResult<VendorOfferingResponse>> UpdateService(Guid id, UpdateVendorOfferingRequest request)
    {
        try
        {
            var result = await _offeringService.UpdateAsync(GetCurrentUserId(), id, request);
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
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("me/services/{id:guid}")]
    public async Task<IActionResult> DeleteService(Guid id)
    {
        try
        {
            await _offeringService.DeleteAsync(GetCurrentUserId(), id);
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

    [HttpGet("me/availability")]
    public async Task<ActionResult<IReadOnlyList<VendorAvailabilityResponse>>> ListMyAvailability()
    {
        try
        {
            var result = await _availabilityService.ListMineAsync(GetCurrentUserId());
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("me/availability")]
    public async Task<ActionResult<VendorAvailabilityResponse>> CreateAvailability(CreateVendorAvailabilityRequest request)
    {
        try
        {
            var result = await _availabilityService.CreateAsync(GetCurrentUserId(), request);
            return CreatedAtAction(nameof(ListMyAvailability), value: result);
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

    [HttpPut("me/availability/{id:guid}")]
    public async Task<ActionResult<VendorAvailabilityResponse>> UpdateAvailability(Guid id, UpdateVendorAvailabilityRequest request)
    {
        try
        {
            var result = await _availabilityService.UpdateAsync(GetCurrentUserId(), id, request);
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
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("me/availability/{id:guid}")]
    public async Task<IActionResult> DeleteAvailability(Guid id)
    {
        try
        {
            await _availabilityService.DeleteAsync(GetCurrentUserId(), id);
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
