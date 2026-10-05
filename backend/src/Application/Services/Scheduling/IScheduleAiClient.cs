using Domain.Entities;

namespace Application.Services.Scheduling;

public interface IScheduleAiClient
{
    Task<AiScheduleGenerationResult> GenerateScheduleAsync(
        Event eventEntity,
        CancellationToken cancellationToken = default);
}

public sealed class AiScheduleGenerationResult
{
    public List<AiScheduleActivity> Activities { get; init; } = [];
    public List<string> Conflicts { get; init; } = [];
}

public sealed class AiScheduleActivity
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
}
