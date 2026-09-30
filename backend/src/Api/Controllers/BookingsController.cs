using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Dtos.Bookings;
using Application.Services.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookings;
    private readonly IVendorRatingService _ratings;

    public BookingsController(IBookingService bookings, IVendorRatingService ratings)
    {
        _bookings = bookings;
        _ratings = ratings;
    }

    [HttpGet("mine")]
    [Authorize(Policy = "EventPlannerOnly")]
    public async Task<ActionResult<IReadOnlyList<BookingResponse>>> ListMine()
    {
        return Ok(await _bookings.ListMineAsync(GetCurrentUserId()));
    }

    [HttpGet("vendor")]
    [Authorize(Policy = "VendorOnly")]
    public async Task<ActionResult<IReadOnlyList<BookingResponse>>> ListForVendor()
    {
        try
        {
            return Ok(await _bookings.ListForVendorAsync(GetCurrentUserId()));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingResponse>> GetById(Guid id)
    {
        try
        {
            var result = await _bookings.GetByIdAsync(
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

    [HttpPut("{id:guid}/complete")]
    [Authorize(Policy = "VendorOnly")]
    public async Task<ActionResult<BookingResponse>> Complete(Guid id)
    {
        try
        {
            return Ok(await _bookings.CompleteAsync(GetCurrentUserId(), id));
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
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<ActionResult<BookingResponse>> Cancel(Guid id, CancelBookingRequest request)
    {
        try
        {
            return Ok(await _bookings.CancelAsync(
                GetCurrentUserId(),
                id,
                request,
                User.IsInRole("VENDOR")));
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

    [HttpPost("{id:guid}/review")]
    [Authorize(Policy = "EventPlannerOnly")]
    public async Task<ActionResult<VendorRatingResponse>> CreateReview(Guid id, CreateVendorRatingRequest request)
    {
        try
        {
            return Ok(await _ratings.CreateAsync(GetCurrentUserId(), id, request));
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

    [HttpGet("{id:guid}/review")]
    public async Task<ActionResult<VendorRatingResponse>> GetReview(Guid id)
    {
        try
        {
            return Ok(await _ratings.GetByBookingIdAsync(
                id,
                GetCurrentUserId(),
                User.IsInRole("VENDOR")));
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
