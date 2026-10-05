using Application.Common.Interfaces;
using Application.Dtos.Admin;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class AdminScheduleRepository(AppDbContext db) : IAdminScheduleRepository
{
    public async Task<(IReadOnlyList<AdminScheduleListItemResponse> Items, int TotalCount)> ListSchedulesAsync(
        AdminScheduleQuery query,
        CancellationToken cancellationToken)
    {
        var schedules =
            from schedule in db.EventSchedules.AsNoTracking()
            join eventEntity in db.Events.AsNoTracking() on schedule.EventId equals eventEntity.Id
            select new { Schedule = schedule, Event = eventEntity };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            schedules = schedules.Where(item => item.Event.EventName.ToLower().Contains(search));
        }

        var total = await schedules.CountAsync(cancellationToken);
        var items = await schedules
            .OrderBy(item => item.Event.PreferredDate)
            .ThenBy(item => item.Event.StartTime)
            .ThenBy(item => item.Event.EventName)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(item => new AdminScheduleListItemResponse
            {
                ScheduleId = item.Schedule.Id,
                EventId = item.Event.Id,
                EventTitle = item.Event.EventName,
                EventDate = item.Event.PreferredDate,
                EventStartTime = item.Event.StartTime,
                EventEndTime = item.Event.EndTime,
                ActivityCount = db.TimelineActivities.Count(activity => activity.ScheduleId == item.Schedule.Id),
                UnresolvedConflictCount = db.ScheduleConflicts.Count(conflict =>
                    conflict.ScheduleId == item.Schedule.Id && !conflict.IsResolved)
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<AdminScheduleDetailResponse?> GetScheduleAsync(
        Guid scheduleId,
        CancellationToken cancellationToken)
    {
        var header = await (
            from schedule in db.EventSchedules.AsNoTracking()
            join eventEntity in db.Events.AsNoTracking() on schedule.EventId equals eventEntity.Id
            where schedule.Id == scheduleId
            select new { Schedule = schedule, Event = eventEntity })
            .FirstOrDefaultAsync(cancellationToken);

        if (header == null)
        {
            return null;
        }

        var planner = await db.Users.AsNoTracking()
            .Where(user => user.Id == header.Event.OwnerId)
            .Select(user => new AdminSchedulePlannerResponse
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            })
            .FirstOrDefaultAsync(cancellationToken);

        var activities = await db.TimelineActivities.AsNoTracking()
            .Where(activity => activity.ScheduleId == scheduleId)
            .OrderBy(activity => activity.StartTime)
            .ThenBy(activity => activity.EndTime)
            .ToListAsync(cancellationToken);

        var vendorIds = activities
            .Where(activity => activity.AssignedVendorId.HasValue)
            .Select(activity => activity.AssignedVendorId!.Value)
            .Distinct()
            .ToList();

        var vendors = await db.Vendors.AsNoTracking()
            .Where(vendor => vendorIds.Contains(vendor.Id))
            .Select(vendor => new AdminScheduleVendorResponse
            {
                Id = vendor.Id,
                BusinessName = vendor.BusinessName,
                Category = vendor.Category.ToString(),
                ContactEmail = vendor.ContactEmail,
                ContactPhone = vendor.ContactPhone,
                Status = vendor.Status.ToString()
            })
            .ToDictionaryAsync(vendor => vendor.Id, cancellationToken);

        var conflicts = await db.ScheduleConflicts.AsNoTracking()
            .Where(conflict => conflict.ScheduleId == scheduleId)
            .OrderBy(conflict => conflict.IsResolved)
            .ThenByDescending(conflict => conflict.DetectedAt)
            .Select(conflict => new AdminScheduleConflictResponse
            {
                Id = conflict.Id,
                ActivityId1 = conflict.ActivityId1,
                ActivityId2 = conflict.ActivityId2,
                ConflictType = conflict.ConflictType,
                Description = conflict.Description,
                IsResolved = conflict.IsResolved,
                DetectedAt = conflict.DetectedAt
            })
            .ToListAsync(cancellationToken);

        return new AdminScheduleDetailResponse
        {
            ScheduleId = header.Schedule.Id,
            Event = new AdminScheduleEventResponse
            {
                EventId = header.Event.Id,
                EventTitle = header.Event.EventName,
                EventDate = header.Event.PreferredDate,
                EventStartTime = header.Event.StartTime,
                EventEndTime = header.Event.EndTime,
                EventStatus = header.Event.Status.ToString(),
                Planner = planner
            },
            Activities = activities.Select(activity => new AdminScheduleActivityResponse
            {
                Id = activity.Id,
                Title = activity.Title,
                Description = activity.Description,
                StartTime = activity.StartTime,
                EndTime = activity.EndTime,
                Status = activity.Status,
                AssignedVendor = activity.AssignedVendorId.HasValue
                    && vendors.TryGetValue(activity.AssignedVendorId.Value, out var vendor)
                        ? vendor
                        : null
            }).ToList(),
            Conflicts = conflicts
        };
    }
}
