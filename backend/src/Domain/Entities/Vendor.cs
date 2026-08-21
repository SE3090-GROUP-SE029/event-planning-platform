using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Microsoft.VisualBasic;

namespace Domain.Entities;

public class Vendor
{
    [Key]
    private int Id {set; get; }
    private int UserId { set; get; }
    private string BusinessName { set; get; }
    private string BusinessDescription { set; get; }
    private string ServiceArea { get; set; } //geographic locations they serve, e.g., "California Bay Area", or a list of cities
    private decimal AverageRating { get; set; }
    private int TotalReviewCount { get; set; }
    private string PhoneNumber { get; set; }
    private string Website { get; set; }
    private VendorStatus ApprovalStatus { get; set; }
    private DateAndTime ApprovedAt { get; set; }
    private int ApprovedByAdminUserId { get; set; }
    private DateAndTime CreatedAt { get; set; }
    private DateAndTime UpdatedAt { get; set; }
}