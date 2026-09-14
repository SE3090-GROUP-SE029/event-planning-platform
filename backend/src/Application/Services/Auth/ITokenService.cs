using System.Security.Claims;
using Domain.Entities;

namespace Application.Services.Auth;

public interface ITokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles, out DateTime expiresAt);
    string GenerateRefreshToken();
    string HashToken(string token);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}