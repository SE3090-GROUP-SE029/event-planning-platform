using Application.Services.Auth;
using Application.Dtos.Auth;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using System.Net;
using System.Data.Common;

namespace Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IRefreshTokenRepository _refreshRepo;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly JwtSettings _jwtSettings;

    public AuthService(IUserRepository userRepo, IRefreshTokenRepository refreshRepo, ITokenService tokenService, IPasswordHasher passwordHasher, Microsoft.Extensions.Options.IOptions<JwtSettings> jwtSettings)
    {
        _userRepo = userRepo;
        _refreshRepo = refreshRepo;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existing = await _userRepo.GetByEmailAsync(request.Email);
        if (existing is not null) throw new InvalidOperationException("A user with this email already exists.");

        if (!Enum.TryParse<RoleName>(request.Role, true, out var roleName))
        {
            throw new Exception($"Role '{request.Role}' does not exist.");
        }

        var role = await _userRepo.GetRoleByNameAsync(roleName) ?? throw new Exception("Role not found.");

        if (role.RoleName == RoleName.ADMIN)
        { 
            throw new InvalidOperationException("Cannot self-register as Admin.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        user.UserRoles.Add(new UserRole {Id = Guid.NewGuid(), User = user, Role = role} );

        await _userRepo.AddAsync(user);
        await _userRepo.SaveChangesAsync();

        return await IssueTokensAsync(user, new[] {role.RoleName}, ipAddress: null);

    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress)
    {
        var user = await _userRepo.GetByEmailAsync(request.Email.Trim().ToLowerInvariant())
            ?? throw new UnauthorizedAccessException("Invalid email or password");

        if (!user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");
        
        var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();
        return await IssueTokensAsync(user, roles, ipAddress);
    }

    public async Task<AuthResponse> RefreshAsync(string RefreshToken, string? ipAddress)
    {
        var hash = _tokenService.HashToken(RefreshToken);
        var stored = await _refreshRepo.GetByTokenHashAsync(hash)
            ?? throw new UnauthorizedAccessException("Invalid refresh token");

        if (!stored.IsActive) throw new UnauthorizedAccessException("Refresh token is expired or revoked");

        stored.RevokedAt = DateTime.UtcNow;

        var roles = stored.User.UserRoles.Select(ur => ur.Role.RoleName).ToList();
        var response = await IssueTokensAsync(stored.User, roles, ipAddress);

        stored.ReplacedByTokenHash = _tokenService.HashToken(response.RefreshToken);
        await _refreshRepo.SaveChangesAsync();

        return response;
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var hash = _tokenService.HashToken(refreshToken);
        var stored = await _refreshRepo.GetByTokenHashAsync(hash);
        
        if(stored is null || !stored.IsActive) return;

        stored.RevokedAt = DateTime.UtcNow;
        await _refreshRepo.SaveChangesAsync();
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, IEnumerable<RoleName> roles, string? ipAddress)
    {
        var roleStrings = roles.Select(r => r.ToString()).ToList();
        var accessToken = _tokenService.GenerateAccessToken(user, roleStrings, out var expiresAt);
        var refreshToken = _tokenService.GenerateRefreshToken();
        
        await _refreshRepo.AddAsync(new RefreshToken{
            Id = Guid.NewGuid(),
            User = user,
            TokenHash = _tokenService.HashToken(refreshToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays),
            IpAddress = ipAddress 
        });

        await _refreshRepo.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email,
            Roles = roleStrings
        };
    } 
}