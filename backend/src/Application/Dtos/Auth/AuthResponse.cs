namespace Application.Dtos.Auth;

public class AuthResponse
{
    public string AccessToken { get; set; } = default!;
    public string RefreshToken {get; set; } = default!;
    public DateTime AccessTokenExpiresAt {get; set;}
    public Guid UserId {get; set;}
    public string Email {get; set;} = default!;
    public List<string> Roles {get; set;} = new();

}