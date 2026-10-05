namespace Infrastructure.Services.Planning;

public sealed class AgenticAiOptions
{
    public const int RequestTimeoutSeconds = 1800;

    public string BaseUrl { get; set; } = "http://localhost:8000";
    public int TimeoutSeconds { get; set; } = RequestTimeoutSeconds;
    public string GeneratePath { get; set; } = "/api/coordinator/generate";
    public string ScheduleGeneratePath { get; set; } = "/api/schedules/generate";
    public string VendorRecommendPath { get; set; } = "/api/vendor-analysis/recommend";
}
