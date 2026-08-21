using System.ComponentModel.DataAnnotations;
using Microsoft.VisualBasic;

namespace Domain.Entities;

public class EventSchedule
{
    [Key]
    private int Id { set; get; }
    private int EventId { set; get; }
    private string ScheduleName { set; get; }
    private string ScheduleVersion { set; get; }
    private bool IsLocked { set; get; }
    private DateAndTime LockedAt { set; get; }
    private int LockedByUserId { set; get; }
    private DateAndTime CreatedAt { set; get; }
    private DateAndTime UpdatedAt { set; get; }
}