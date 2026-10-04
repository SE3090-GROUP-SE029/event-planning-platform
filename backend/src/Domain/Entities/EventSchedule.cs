using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class EventSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public bool IsLocked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TimelineActivity> Activities { get; set; } = new List<TimelineActivity>();
    public ICollection<ScheduleConflict> Conflicts { get; set; } = new List<ScheduleConflict>();

    [NotMapped]
    public string? EventName { get; set; }

    [NotMapped]
    public DateTime? EventDate { get; set; }

    [NotMapped]
    public TimeOnly? EventStartTime { get; set; }

    [NotMapped]
    public TimeOnly? EventEndTime { get; set; }
}
