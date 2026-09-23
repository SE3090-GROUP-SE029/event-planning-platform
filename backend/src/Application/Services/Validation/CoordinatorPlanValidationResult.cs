using System.Text.Json.Serialization;

namespace Application.Services.Validation;

public sealed record CoordinatorPlanValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
    public string Summary { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string ValidationMethod { get; init; } = "CoordinatorPlanValidation";
}
