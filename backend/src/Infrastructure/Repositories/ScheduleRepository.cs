using Application.Services.Scheduling;
using Domain.Entities;
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

    public async Task<List<TimelineActivity>> GetActivitiesByScheduleIdAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        return await _context.TimelineActivities
            .Where(a => a.ScheduleId == scheduleId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddConflictsAsync(IEnumerable<ScheduleConflict> conflicts, CancellationToken cancellationToken = default)
    {
        _context.ScheduleConflicts.AddRange(conflicts);
        await _context.SaveChangesAsync(cancellationToken);
    }
}