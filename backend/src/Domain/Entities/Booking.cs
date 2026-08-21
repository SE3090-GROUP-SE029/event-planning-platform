using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Microsoft.VisualBasic;

namespace Domain.Entities;

public class Booking
{
    [Key]
    private int Id { set; get; }
    private int EventId { set; get; }
    private int VendorId { set; get; }
    private int ServiceId { set; get; }
    private int QuotationId { set; get; }
    private int BookedByUserId { set; get; }
    private decimal FinalPrice { set; get; }
    private BookingStatus BookingStatus { set; get; }
    private DateAndTime EventDate { set; get; }
    private DateAndTime ConfirmedAt { set; get; }
    private string VendorNotes { set; get; }
    private string PlannerNotes { set; get; }
    private string CancellationReason { set; get; }
    private DateAndTime CancelledAt { set; get; }
    private DateAndTime CreatedAt { set; get; }
    private DateAndTime UpdatedAt { set; get; }
}