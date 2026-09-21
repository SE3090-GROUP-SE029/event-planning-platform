namespace Domain.Entities;

public class ScheduleConflict
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ScheduleId { get; set; }
    public EventSchedule Schedule { get; set; } = null!;

    public Guid ActivityId1 { get; set; }
    public Guid ActivityId2 { get; set; }

    public string ConflictType { get; set; } = "VendorDoubleBooked"; // VendorDoubleBooked, TimeOverlap
    public string Description { get; set; } = string.Empty;
    public bool IsResolved { get; set; } = false;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}