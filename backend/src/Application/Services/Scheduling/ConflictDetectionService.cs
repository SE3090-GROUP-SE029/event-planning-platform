using Domain.Entities;

namespace Application.Services.Scheduling;

public class ConflictDetectionService
{
    public static bool Overlaps(DateTime startA, DateTime endA, DateTime startB, DateTime endB)
    {
        return startA < endB && endA > startB;
    }

    public List<ScheduleConflict> DetectConflicts(Guid scheduleId, List<TimelineActivity> activities)
    {
        var conflicts = new List<ScheduleConflict>();

        for (int i = 0; i < activities.Count; i++)
        {
            for (int j = i + 1; j < activities.Count; j++)
            {
                var act1 = activities[i];
                var act2 = activities[j];

                if (Overlaps(act1.StartTime, act1.EndTime, act2.StartTime, act2.EndTime))
                {
                    if (act1.AssignedVendorId.HasValue && act1.AssignedVendorId == act2.AssignedVendorId)
                    {
                        conflicts.Add(new ScheduleConflict
                        {
                            ScheduleId = scheduleId,
                            ActivityId1 = act1.Id,
                            ActivityId2 = act2.Id,
                            ConflictType = "VendorDoubleBooked",
                            Description = $"Vendor {act1.AssignedVendorId} is assigned to overlapping activities: '{act1.Title}' and '{act2.Title}'.",
                            IsResolved = false,
                            DetectedAt = DateTime.UtcNow
                        });
                    }
                }
            }
        }

        return conflicts;
    }
}