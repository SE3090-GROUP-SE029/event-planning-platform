using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Microsoft.VisualBasic;

namespace Domain.Entities;

public class EventStatusHistory
{
    [Key]
    private int Id { set; get; }
    private int EventId { set; get; }
    private EventStatus OldStatus { set; get; }
    private EventStatus NewStatus { set; get; }
    private int ChangedByUserId { set; get; }
    private string ChangeReason { set; get; }
    private DateAndTime ChangedAt { set; get; }
}