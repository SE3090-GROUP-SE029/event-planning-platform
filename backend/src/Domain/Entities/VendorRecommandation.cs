using System.ComponentModel.DataAnnotations;
using Microsoft.VisualBasic;

namespace Domain.Entities;

public class VendorRecommandation
{
    [Key]
    private int Id { set; get; }
    private int EnventId { set; get; }
    private int VendorId { set; get; }
    private int Rank { set; get; }
    private decimal SuitabilityScore { set; get; }
    private string MatchedCategories { set; get; }
    private string Rationale { set; get; }
    private DateAndTime GeneratedAt { set; get; }
    private int ReviewedByUserId { set; get; }
    private DateAndTime ReviewedAt { set; get; }
}