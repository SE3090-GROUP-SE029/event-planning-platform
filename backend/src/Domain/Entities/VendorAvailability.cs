using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Microsoft.VisualBasic;

namespace Domain.Entities;

public class VendorAvailability
{
    [Key]
    private int Id {set; get; }
    private int VendorId { set; get; }
    private DateAndTime StartDate { set; get; }
    private DateAndTime EndDate { set; get; }
    private AvailabilityStatus availabilityStatus { set; get; }
    private string Notes{ set; get; }
    private DateAndTime CreatedAt { set; get; }
    private DateAndTime UpdatedAt { set; get; }
}