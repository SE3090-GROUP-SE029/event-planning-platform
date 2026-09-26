using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Auth;

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var value = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? accessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }

    public bool IsAdmin => accessor.HttpContext?.User.IsInRole("ADMIN") == true;
}
