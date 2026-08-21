using System.ComponentModel.DataAnnotations;
using Microsoft.VisualBasic;

namespace Domain.Entities;

public class EventPlanDraft
{
    [Key]
    private int Id { set; get; }
    private int EventId { set; get; }
    private string ServiceCategories { set; get; }
    private string BudgetAllocation { set; get; }
    private string TargetVendorTypes { set; get; }
    private string ProposedTimeLine { set; get; }
    private string AIAgent { set; get; }
    private DateAndTime GeneratedAt { set; get; }
    private DateAndTime LastReviewedAt { set; get; }
    private int ReviewedByUserId { set; get; }
}