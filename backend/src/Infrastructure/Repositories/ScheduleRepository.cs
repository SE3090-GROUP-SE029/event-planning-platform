using Application.Services.Scheduling;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ScheduleRepository : IScheduleRepository
{
    private readonly AppDbContext _context;

    public ScheduleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EventSchedule?> GetByIdAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        return await _context.EventSchedules
            .Include(s => s.Activities)
            .Include(s => s.Conflicts)
            .FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);
    }

    public async Task<EventSchedule?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.EventSchedules
            .Include(s => s.Activities)
            .Include(s => s.Conflicts)
            .FirstOrDefaultAsync(s => s.EventId == eventId, cancellationToken);
    }

    public async Task<EventSchedule> CreateScheduleAsync(EventSchedule schedule, CancellationToken cancellationToken = default)
    {
        _context.EventSchedules.Add(schedule);
        await _context.SaveChangesAsync(cancellationToken);
        return schedule;
    }

    public async Task<TimelineActivity> AddActivityAsync(TimelineActivity activity, CancellationToken cancellationToken = default)
    {
        _context.TimelineActivities.Add(activity);
        await _context.SaveChangesAsync(cancellationToken);
        return activity;
    }

    public async Task<TimelineActivity?> GetActivityByIdAsync(Guid activityId, CancellationToken cancellationToken = default)
    {
        return await _context.TimelineActivities
            .Include(a => a.Schedule)
            .FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);
    }

    public async Task<List<TimelineActivity>> GetActivitiesByScheduleIdAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        return await _context.TimelineActivities
            .Where(a => a.ScheduleId == scheduleId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TimelineActivity>> GetActivitiesByVendorIdAsync(Guid vendorId, CancellationToken cancellationToken = default)
    {
        return await _context.TimelineActivities
            .Include(a => a.Schedule)
            .Where(a => a.AssignedVendorId == vendorId)
            .OrderBy(a => a.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ScheduleConflict>> GetConflictsByScheduleIdAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        return await _context.ScheduleConflicts
            .Where(c => c.ScheduleId == scheduleId)
            .OrderByDescending(c => c.DetectedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddConflictsAsync(IEnumerable<ScheduleConflict> conflicts, CancellationToken cancellationToken = default)
    {
        _context.ScheduleConflicts.AddRange(conflicts);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceUnresolvedConflictsAsync(Guid scheduleId, IEnumerable<ScheduleConflict> conflicts, CancellationToken cancellationToken = default)
    {
        var deterministicTypes = new[] { "VendorDoubleBooked" };
        var staleConflicts = await _context.ScheduleConflicts
            .Where(c => c.ScheduleId == scheduleId
                && !c.IsResolved
                && deterministicTypes.Contains(c.ConflictType))
            .ToListAsync(cancellationToken);

        _context.ScheduleConflicts.RemoveRange(staleConflicts);
        _context.ScheduleConflicts.AddRange(conflicts);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TimelineActivity?> UpdateActivityAsync(
        Guid activityId,
        string title,
        string? description,
        DateTime startTime,
        DateTime endTime,
        Guid? assignedVendorId,
        CancellationToken cancellationToken = default)
    {
        var activity = await _context.TimelineActivities.FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);
        if (activity == null)
        {
            return null;
        }

        activity.Title = title;
        activity.Description = description;
        activity.StartTime = startTime;
        activity.EndTime = endTime;
        activity.AssignedVendorId = assignedVendorId;

        await _context.SaveChangesAsync(cancellationToken);
        return activity;
    }

    public async Task<TimelineActivity?> UpdateActivityStatusAsync(Guid activityId, ActivityStatus status, CancellationToken cancellationToken = default)
    {
        var activity = await _context.TimelineActivities.FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);
        if (activity == null)
        {
            return null;
        }

        activity.Status = status.ToString();

        await _context.SaveChangesAsync(cancellationToken);
        return activity;
    }

    public async Task<bool> DeleteActivityAsync(Guid activityId, CancellationToken cancellationToken = default)
    {
        var activity = await _context.TimelineActivities.FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);
        if (activity == null)
        {
            return false;
        }

        _context.TimelineActivities.Remove(activity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
