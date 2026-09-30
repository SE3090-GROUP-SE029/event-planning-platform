using System.Globalization;
using Application.DTOs.Scheduling;
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
    private readonly IVendorRepository _vendorRepository;

    public ScheduleService(
        IScheduleRepository scheduleRepository,
        ConflictDetectionService conflictDetector,
        IEventRepository eventRepository,
        IScheduleAiClient scheduleAiClient,
        IVendorRepository vendorRepository)
    {
        _scheduleRepository = scheduleRepository;
        _conflictDetector = conflictDetector;
        _eventRepository = eventRepository;
        _scheduleAiClient = scheduleAiClient;
        _vendorRepository = vendorRepository;
    }

    public async Task<EventSchedule> GetOrCreateScheduleForPlannerAsync(
        Guid eventId,
        Guid plannerUserId,
        CancellationToken cancellationToken = default)
    {
        await RequirePlannerEventAsync(eventId, plannerUserId);

        var schedule = await _scheduleRepository.GetByEventIdAsync(eventId, cancellationToken);

        if (schedule == null)
        {
            schedule = new EventSchedule { EventId = eventId };
            schedule = await _scheduleRepository.CreateScheduleAsync(schedule, cancellationToken);
        }

        return schedule;
    }

    public async Task<TimelineActivity> AddActivityForPlannerAsync(
        Guid scheduleId, 
        Guid plannerUserId,
        string title, 
        string? description, 
        DateTime startTime, 
        DateTime endTime, 
        Guid? vendorId,
        CancellationToken cancellationToken = default)
    {
        await RequirePlannerScheduleAsync(scheduleId, plannerUserId, cancellationToken);
        await ValidateActivityFieldsAsync(title, startTime, endTime, vendorId);

        var activity = new TimelineActivity
        {
            ScheduleId = scheduleId,
            Title = title.Trim(),
            Description = NormalizeDescription(description),
            StartTime = startTime,
            EndTime = endTime,
            AssignedVendorId = vendorId,
            Status = ActivityStatus.SCHEDULED.ToString()
        };

        activity = await _scheduleRepository.AddActivityAsync(activity, cancellationToken);

        await RecalculateDeterministicConflictsAsync(scheduleId, cancellationToken);

        return activity;
    }

    public async Task<TimelineActivity?> UpdateActivityForPlannerAsync(
        Guid activityId,
        Guid plannerUserId,
        string title,
        string? description,
        DateTime startTime,
        DateTime endTime,
        Guid? vendorId,
        CancellationToken cancellationToken = default)
    {
        var activity = await _scheduleRepository.GetActivityByIdAsync(activityId, cancellationToken);
        if (activity == null)
        {
            return null;
        }

        await RequirePlannerScheduleAsync(activity.ScheduleId, plannerUserId, cancellationToken);
        await ValidateActivityFieldsAsync(title, startTime, endTime, vendorId);

        var updated = await _scheduleRepository.UpdateActivityAsync(
            activityId,
            title.Trim(),
            NormalizeDescription(description),
            startTime,
            endTime,
            vendorId,
            cancellationToken);

        await RecalculateDeterministicConflictsAsync(activity.ScheduleId, cancellationToken);
        return updated;
    }

    public async Task<bool?> DeleteActivityForPlannerAsync(
        Guid activityId,
        Guid plannerUserId,
        CancellationToken cancellationToken = default)
    {
        var activity = await _scheduleRepository.GetActivityByIdAsync(activityId, cancellationToken);
        if (activity == null)
        {
            return null;
        }

        await RequirePlannerScheduleAsync(activity.ScheduleId, plannerUserId, cancellationToken);

        var deleted = await _scheduleRepository.DeleteActivityAsync(activityId, cancellationToken);
        if (deleted)
        {
            await RecalculateDeterministicConflictsAsync(activity.ScheduleId, cancellationToken);
        }

        return deleted;
    }

    public async Task<TimelineActivity?> UpdateActivityStatusForPlannerAsync(
        Guid activityId, 
        Guid plannerUserId,
        ActivityStatus status, 
        CancellationToken cancellationToken = default)
    {
        var activity = await _scheduleRepository.GetActivityByIdAsync(activityId, cancellationToken);
        if (activity == null)
        {
            return null;
        }

        await RequirePlannerScheduleAsync(activity.ScheduleId, plannerUserId, cancellationToken);
        return await _scheduleRepository.UpdateActivityStatusAsync(activityId, status, cancellationToken);
    }

    public async Task<TimelineActivity?> UpdateActivityStatusForVendorAsync(
        Guid activityId,
        Guid vendorUserId,
        ActivityStatus status,
        CancellationToken cancellationToken = default)
    {
        if (!IsVendorOperationalStatus(status))
        {
            throw new ArgumentException("Vendors can only set Scheduled, In Progress, Completed, or Skipped statuses.");
        }

        var vendor = await RequireVendorAsync(vendorUserId);
        var activity = await _scheduleRepository.GetActivityByIdAsync(activityId, cancellationToken);
        if (activity == null)
        {
            return null;
        }

        if (activity.AssignedVendorId != vendor.Id)
        {
            throw new UnauthorizedAccessException("You are not authorized to update this activity.");
        }

        return await _scheduleRepository.UpdateActivityStatusAsync(activityId, status, cancellationToken);
    }

    public async Task<IReadOnlyList<VendorScheduleActivityResponse>> GetAssignedActivitiesForVendorAsync(
        Guid vendorUserId,
        CancellationToken cancellationToken = default)
    {
        var vendor = await RequireVendorAsync(vendorUserId);
        var activities = await _scheduleRepository.GetActivitiesByVendorIdAsync(vendor.Id, cancellationToken);

        return activities
            .Select(activity => new VendorScheduleActivityResponse(
                activity.Id,
                activity.ScheduleId,
                activity.Schedule.EventId,
                activity.Title,
                activity.Description,
                activity.StartTime,
                activity.EndTime,
                activity.Status))
            .ToList();
    }

    public async Task<IReadOnlyList<ScheduleConflict>> GetConflictsForPlannerAsync(
        Guid scheduleId,
        Guid plannerUserId,
        CancellationToken cancellationToken = default)
    {
        await RequirePlannerScheduleAsync(scheduleId, plannerUserId, cancellationToken);
        return await _scheduleRepository.GetConflictsByScheduleIdAsync(scheduleId, cancellationToken);
    }

    public async Task<EventSchedule> GenerateScheduleWithAiForPlannerAsync(
        Guid scheduleId,
        Guid plannerUserId,
        CancellationToken cancellationToken = default)
    {
        var schedule = await RequirePlannerScheduleAsync(scheduleId, plannerUserId, cancellationToken);

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

        await RecalculateDeterministicConflictsAsync(scheduleId, cancellationToken);

        schedule.Activities = generatedActivities;
        schedule.Conflicts = allConflicts;
        schedule.UpdatedAt = DateTime.UtcNow;
        return schedule;
    }

    private async Task<Event> RequirePlannerEventAsync(Guid eventId, Guid plannerUserId)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(eventId)
            ?? throw new KeyNotFoundException("Event not found.");

        if (eventEntity.OwnerId != plannerUserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to access this event schedule.");
        }

        return eventEntity;
    }

    private async Task<EventSchedule> RequirePlannerScheduleAsync(
        Guid scheduleId,
        Guid plannerUserId,
        CancellationToken cancellationToken)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Schedule {scheduleId} was not found.");

        await RequirePlannerEventAsync(schedule.EventId, plannerUserId);
        return schedule;
    }

    private async Task<Vendor> RequireVendorAsync(Guid vendorUserId)
    {
        return await _vendorRepository.GetByUserIdAsync(vendorUserId)
            ?? throw new KeyNotFoundException("Vendor profile not found.");
    }

    private async Task ValidateActivityFieldsAsync(
        string title,
        DateTime startTime,
        DateTime endTime,
        Guid? vendorId)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Activity title is required.");
        }

        if (title.Trim().Length > 150)
        {
            throw new ArgumentException("Activity title must be 150 characters or fewer.");
        }

        if (endTime <= startTime)
        {
            throw new ArgumentException("EndTime must be strictly after StartTime.");
        }

        if (vendorId.HasValue && await _vendorRepository.GetByIdAsync(vendorId.Value) == null)
        {
            throw new ArgumentException("AssignedVendorId must reference an existing vendor.");
        }
    }

    private async Task RecalculateDeterministicConflictsAsync(
        Guid scheduleId,
        CancellationToken cancellationToken)
    {
        var activities = await _scheduleRepository.GetActivitiesByScheduleIdAsync(scheduleId, cancellationToken);
        var detectedConflicts = _conflictDetector.DetectConflicts(scheduleId, activities);
        await _scheduleRepository.ReplaceUnresolvedConflictsAsync(scheduleId, detectedConflicts, cancellationToken);
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    private static bool IsVendorOperationalStatus(ActivityStatus status) =>
        status is ActivityStatus.SCHEDULED
            or ActivityStatus.IN_PROGRESS
            or ActivityStatus.COMPLETED
            or ActivityStatus.SKIPPED;

    private static DateTime ParseTimestamp(string value, string fieldName)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"AI schedule returned an invalid {fieldName} timestamp: {value}");
    }
}
