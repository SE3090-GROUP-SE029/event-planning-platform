using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Domain.Entities;
using Infrastructure.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Backend.UnitTests;

public sealed class TokenServiceTests
{
    [Fact]
    public void GenerateAccessToken_ValidatesWithConfiguredJwtOptions()
    {
        var options = new JwtOptions
        {
            Secret = new string('k', 32),
            Issuer = "unit-test-issuer",
            Audience = "unit-test-audience"
        };
        var tokenService = new TokenService(Options.Create(options));
        var user = new User { Id = Guid.NewGuid(), Email = "planner@example.com" };

        var token = tokenService.GenerateAccessToken(user, ["EVENT_PLANNER"], out var expiresAt);
        var principal = new JwtSecurityTokenHandler().ValidateToken(
            token,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = options.Issuer,
                ValidateAudience = true,
                ValidAudience = options.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret)),
                ValidateLifetime = true
            },
            out _);

        Assert.Equal(user.Id.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("EVENT_PLANNER", principal.FindFirst(ClaimTypes.Role)?.Value);
        Assert.True(expiresAt > DateTime.UtcNow);
    }
}
