using System.ComponentModel.DataAnnotations;
using Microsoft.VisualBasic;

namespace Domain.Entities;

public class TimeSlot
{
    [Key]
    private int Id { set; get; }
    private int ActivityId { set; get; }
    private TimeOnly StartTime { set; get; }
    private TimeOnly EndTime { set; get; }
    private bool IsProposed { set; get; }
    private DateAndTime CreatedAt { set; get; }
    private DateAndTime UpdatedAt { set; get; }
}