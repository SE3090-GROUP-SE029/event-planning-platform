using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Microsoft.VisualBasic;

namespace Domain.Entities;

public class TimelineActivity
{
    [Key]
    private int Id { set; get; }
    private int ScheduleId { set; get; }
    private string ActivityName { set; get; }
    private string ActivityDescription{ set; get; }
    private int SequanceOrder { set; get; }
    private int AssignedBookingId { set; get; }
    private string AssignedVenueLocation { set; get; }
    private ActivityStatus ActivityStatus { set; get; }
    private string Notes { set; get; }
    private DateAndTime CreatedAt { set; get; }
    private DateAndTime UpdatedAt { set; get; }
}