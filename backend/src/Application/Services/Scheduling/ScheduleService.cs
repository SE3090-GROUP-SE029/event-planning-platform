using Domain.Entities;

namespace Application.Services.Scheduling;

public class ScheduleService
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly ConflictDetectionService _conflictDetector;

    public ScheduleService(IScheduleRepository scheduleRepository, ConflictDetectionService conflictDetector)
    {
        _scheduleRepository = scheduleRepository;
        _conflictDetector = conflictDetector;
    }

    public async Task<EventSchedule> GetOrCreateScheduleAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var schedule = await _scheduleRepository.GetByEventIdAsync(eventId, cancellationToken);

        if (schedule == null)
        {
            schedule = new EventSchedule { EventId = eventId };
            schedule = await _scheduleRepository.CreateScheduleAsync(schedule, cancellationToken);
        }

        return schedule;
    }

    public async Task<TimelineActivity> AddActivityAsync(
        Guid scheduleId, 
        string title, 
        string? description, 
        DateTime startTime, 
        DateTime endTime, 
        Guid? vendorId,
        CancellationToken cancellationToken = default)
    {
        var activity = new TimelineActivity
        {
            ScheduleId = scheduleId,
            Title = title,
            Description = description,
            StartTime = startTime,
            EndTime = endTime,
            AssignedVendorId = vendorId,
            Status = "Scheduled"
        };

        activity = await _scheduleRepository.AddActivityAsync(activity, cancellationToken);

        var activities = await _scheduleRepository.GetActivitiesByScheduleIdAsync(scheduleId, cancellationToken);
        var detectedConflicts = _conflictDetector.DetectConflicts(scheduleId, activities);

        if (detectedConflicts.Count != 0)
        {
            await _scheduleRepository.AddConflictsAsync(detectedConflicts, cancellationToken);
        }

        return activity;
    }
}