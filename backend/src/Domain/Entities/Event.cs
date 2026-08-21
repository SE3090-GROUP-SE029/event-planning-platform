using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Microsoft.VisualBasic;

namespace Domain.Entities;
public class Event
{
    [Key]
    private int Id { set; get; }
    private int CreatedByUserId { set; get; }
    private EventType EventType { get; set; }
    private string EventName { get; set; }
    private DateAndTime EventStartDate { set; get; }
    private DateAndTime EventEndDate { set; get; }
    private int EstimatedGuestCount { set; get; }
    private decimal BudgetAmount { set; get; }
    private  VenueType PrefferedVenueType { set; get; }
    private string PrefferedLocation { set; get; }
    private string RequirementNotes { set; get; }
    private EventStatus Status { get; set; }
    private DateAndTime CreatedAt { set; get; }
    private DateAndTime UpdatedAt { set; get; }
    private DateAndTime CompletedAt { set; get; }
}