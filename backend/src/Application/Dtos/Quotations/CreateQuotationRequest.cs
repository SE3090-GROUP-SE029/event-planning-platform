namespace Application.Dtos.Quotations;

public class CreateQuotationRequest
{
    public Guid VendorId { get; set; }
    public Guid VendorServiceId { get; set; }
    public Guid EventId { get; set; }
    public DateTime RequestedStartDateTime { get; set; }
    public DateTime RequestedEndDateTime { get; set; }
    public string? CustomerMessage { get; set; }
}
