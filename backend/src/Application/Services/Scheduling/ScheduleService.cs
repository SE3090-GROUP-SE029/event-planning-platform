using System.Globalization;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Scheduling;

public class ScheduleService
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly ConflictDetectionService _conflictDetector;
    private readonly IEventRepository _eventRepository;
    private readonly IScheduleAiClient _scheduleAiClient;

    public ScheduleService(
        IScheduleRepository scheduleRepository,
        ConflictDetectionService conflictDetector,
        IEventRepository eventRepository,
        IScheduleAiClient scheduleAiClient)
    {
        _scheduleRepository = scheduleRepository;
        _conflictDetector = conflictDetector;
        _eventRepository = eventRepository;
        _scheduleAiClient = scheduleAiClient;
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
            Status = ActivityStatus.SCHEDULED.ToString()
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

    public async Task<TimelineActivity?> UpdateActivityStatusAsync(
        Guid activityId, 
        ActivityStatus status, 
        CancellationToken cancellationToken = default)
    {
        return await _scheduleRepository.UpdateActivityStatusAsync(activityId, status, cancellationToken);
    }

    public async Task<EventSchedule> GenerateScheduleWithAiAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Schedule {scheduleId} was not found.");

        var eventEntity = await _eventRepository.GetByIdAsync(schedule.EventId)
            ?? throw new KeyNotFoundException($"Event {schedule.EventId} associated with schedule {scheduleId} was not found.");

        var aiResponse = await _scheduleAiClient.GenerateScheduleAsync(eventEntity, cancellationToken);

        var generatedActivities = new List<TimelineActivity>();
        foreach (var activityDto in aiResponse.Activities)
        {
            var activity = new TimelineActivity
            {
                ScheduleId = scheduleId,
                Title = activityDto.Title,
                Description = activityDto.Description,
                StartTime = ParseTimestamp(activityDto.StartTime, "start_time"),
                EndTime = ParseTimestamp(activityDto.EndTime, "end_time"),
                AssignedVendorId = null,
                Status = ActivityStatus.SCHEDULED.ToString()
            };

            activity = await _scheduleRepository.AddActivityAsync(activity, cancellationToken);
            generatedActivities.Add(activity);
        }

        var generatedConflicts = aiResponse.Conflicts
            .Select(conflict => new ScheduleConflict
            {
                ScheduleId = scheduleId,
                ActivityId1 = generatedActivities.FirstOrDefault()?.Id ?? Guid.Empty,
                ActivityId2 = Guid.Empty,
                ConflictType = "AIGenerated",
                Description = conflict,
                IsResolved = false,
                DetectedAt = DateTime.UtcNow
            })
            .ToList();

        var detectedConflicts = _conflictDetector.DetectConflicts(scheduleId, generatedActivities);
        var allConflicts = generatedConflicts.Concat(detectedConflicts).ToList();

        if (allConflicts.Count != 0)
        {
            await _scheduleRepository.AddConflictsAsync(allConflicts, cancellationToken);
        }

        schedule.Activities = generatedActivities;
        schedule.Conflicts = allConflicts;
        schedule.UpdatedAt = DateTime.UtcNow;
        return schedule;
    }

    private static DateTime ParseTimestamp(string value, string fieldName)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"AI schedule returned an invalid {fieldName} timestamp: {value}");
    }
}