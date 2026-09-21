using Domain.Entities;

namespace Application.Services.Scheduling;

public interface IScheduleRepository
{
    Task<EventSchedule?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<EventSchedule> CreateScheduleAsync(EventSchedule schedule, CancellationToken cancellationToken = default);
    Task<TimelineActivity> AddActivityAsync(TimelineActivity activity, CancellationToken cancellationToken = default);
    Task<List<TimelineActivity>> GetActivitiesByScheduleIdAsync(Guid scheduleId, CancellationToken cancellationToken = default);
    Task AddConflictsAsync(IEnumerable<ScheduleConflict> conflicts, CancellationToken cancellationToken = default);
}