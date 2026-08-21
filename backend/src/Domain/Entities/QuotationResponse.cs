using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class QuotationResponse
{
    [Key]
    private int Id { set; get; }
    private int QuotationId { set; get; }
    private decimal VendorProposedPrice { set; get; }
    private string VendorNotes { set; get; }
    private string IncludedItems { set; get; }
    private string ExcludedItems { set; get; }
    private string TermsAndConditions { set; get; }
}