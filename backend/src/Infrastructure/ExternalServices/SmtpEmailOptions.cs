namespace Infrastructure.ExternalServices;

public class SmtpEmailOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? FromAddress { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public bool EnableSsl { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 10;
}
