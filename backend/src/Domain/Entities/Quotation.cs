using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Microsoft.VisualBasic;

namespace Domain.Entities;

public class Quotation
{
    [Key]
    private int Id { set; get; }
    private int EventId { set; get; }
    private int VendorId { set; get; }
    private int RequestByUserId { set; get; }
    private DateAndTime StartDate { set; get; }
    private DateAndTime EndDate { set; get; }
    private string SpecialRequests { set; get; }
    private QuotationStatus QuotationStatus { set; get; }
    private DateAndTime RequestedAt { set; get; }
    private DateAndTime RespondedAt { set; get; }
    private DateAndTime ExpiresAt { set; get; }
    private string Notes { set; get; }
}