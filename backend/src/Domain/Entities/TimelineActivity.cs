namespace Domain.Entities;

public class TimelineActivity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ScheduleId { get; set; }
    public EventSchedule Schedule { get; set; } = null!;
    
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public Guid? AssignedVendorId { get; set; }
    public string Status { get; set; } = "Scheduled";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}