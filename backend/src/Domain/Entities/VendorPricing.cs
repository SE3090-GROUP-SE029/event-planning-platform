using System.ComponentModel.DataAnnotations;
using Microsoft.VisualBasic;

namespace Domain.Entities;

public class VendorPricing
{
    [Key]
    private int Id { set; get; }
    private int ServiceId { set; get; }
    private string PackageName { set; get; }
    private decimal BasePrice { set; get; }
    private string Currency { set; get; }
    private string Description { set; get; }
    private int MinGuestCount { set; get; }
    private int MaxGuestCount { set; get; }
    private DateAndTime EffectiveFrom { set; get; }
    private DateAndTime EffectiveTo { set; get; }
    private DateAndTime CreatedAt { set; get; }
    private DateAndTime UpdatedAt { set; get; }
    private bool IsActive { set; get; }
}