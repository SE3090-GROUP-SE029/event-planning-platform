namespace Application.Dtos.Quotations;

public class RespondToQuotationRequest
{
    public decimal QuotedPrice { get; set; }
    public string? VendorTerms { get; set; }
}
