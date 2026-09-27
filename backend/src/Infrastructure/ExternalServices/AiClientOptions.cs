namespace Infrastructure.ExternalServices;

public class AiClientOptions
{
    public bool Enabled { get; set; } = true;
    public string ServiceUrl { get; set; } = "http://127.0.0.1:8000/";
    public int TimeoutSeconds { get; set; } = 120;
    public int PollSeconds { get; set; } = 5;

    public bool IsValid => Uri.TryCreate(ServiceUrl, UriKind.Absolute, out var uri) && uri.IsLoopback &&
        uri.Scheme is "http" or "https" && string.IsNullOrEmpty(uri.UserInfo) &&
        string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment) && uri.AbsolutePath == "/" &&
        TimeoutSeconds is >= 1 and <= 300 && PollSeconds is >= 1 and <= 60;
}
