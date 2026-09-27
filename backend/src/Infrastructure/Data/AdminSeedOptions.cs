namespace Infrastructure.Data;

public sealed class AdminSeedOptions
{
    public string Email { get; set; } = "admin@planit.com";
    public string FirstName { get; set; } = "System";
    public string LastName { get; set; } = "Administrator";
    public string? Password { get; set; }
}
