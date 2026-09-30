using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Scheduling;

public interface IScheduleRepository
{
    Task<EventSchedule?> GetByIdAsync(Guid scheduleId, CancellationToken cancellationToken = default);
    Task<EventSchedule?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<EventSchedule> CreateScheduleAsync(EventSchedule schedule, CancellationToken cancellationToken = default);
    Task<TimelineActivity> AddActivityAsync(TimelineActivity activity, CancellationToken cancellationToken = default);
    Task<TimelineActivity?> GetActivityByIdAsync(Guid activityId, CancellationToken cancellationToken = default);
    Task<List<TimelineActivity>> GetActivitiesByScheduleIdAsync(Guid scheduleId, CancellationToken cancellationToken = default);
    Task<List<TimelineActivity>> GetActivitiesByVendorIdAsync(Guid vendorId, CancellationToken cancellationToken = default);
    Task<List<ScheduleConflict>> GetConflictsByScheduleIdAsync(Guid scheduleId, CancellationToken cancellationToken = default);
    Task AddConflictsAsync(IEnumerable<ScheduleConflict> conflicts, CancellationToken cancellationToken = default);
    Task ReplaceUnresolvedConflictsAsync(Guid scheduleId, IEnumerable<ScheduleConflict> conflicts, CancellationToken cancellationToken = default);
    Task<TimelineActivity?> UpdateActivityAsync(
        Guid activityId,
        string title,
        string? description,
        DateTime startTime,
        DateTime endTime,
        Guid? assignedVendorId,
        CancellationToken cancellationToken = default);
    Task<TimelineActivity?> UpdateActivityStatusAsync(Guid activityId, ActivityStatus status, CancellationToken cancellationToken = default);
    Task<bool> DeleteActivityAsync(Guid activityId, CancellationToken cancellationToken = default);
}
