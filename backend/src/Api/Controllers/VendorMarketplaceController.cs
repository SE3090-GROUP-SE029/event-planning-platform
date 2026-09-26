using Application.Dtos.Vendors;
using Application.Services.Vendors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/vendors/marketplace")]
[Authorize]
public class VendorMarketplaceController : ControllerBase
{
    private readonly IVendorMarketplaceService _marketplace;

    public VendorMarketplaceController(IVendorMarketplaceService marketplace)
    {
        _marketplace = marketplace;
    }

    [HttpGet]
    public async Task<ActionResult<MarketplaceVendorListResponse>> List([FromQuery] VendorMarketplaceQuery query)
    {
        try
        {
            var result = await _marketplace.ListAsync(query);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{vendorId:guid}")]
    public async Task<ActionResult<MarketplaceVendorDetailResponse>> GetById(Guid vendorId)
    {
        try
        {
            var result = await _marketplace.GetByIdAsync(vendorId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
