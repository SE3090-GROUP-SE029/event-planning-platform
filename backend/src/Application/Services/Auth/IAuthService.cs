using Application.Dtos.Auth;

namespace Application.Services.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress);
    Task<AuthResponse> RefreshAsync(string refreshToken, string? ipAddress);
    Task LogoutAsync(string refreshToken); 
}