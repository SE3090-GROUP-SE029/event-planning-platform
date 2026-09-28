namespace Infrastructure.Services.Planning;

public sealed class AgenticAiOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8000";
    public int TimeoutSeconds { get; set; } = 250;
    public string GeneratePath { get; set; } = "/api/coordinator/generate";
    public string VendorRecommendPath { get; set; } = "/api/vendor-analysis/recommend";
}
