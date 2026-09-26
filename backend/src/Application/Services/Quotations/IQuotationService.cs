using Application.Dtos.Quotations;

namespace Application.Services.Quotations;

public interface IQuotationService
{
    Task<QuotationResponse> CreateAsync(Guid requestedByUserId, CreateQuotationRequest request);
    Task<IReadOnlyList<QuotationResponse>> ListMineAsync(Guid requestedByUserId);
    Task<IReadOnlyList<QuotationResponse>> ListForVendorAsync(Guid vendorUserId);
    Task<QuotationResponse> GetByIdAsync(Guid quotationId, Guid userId, bool isVendor);
    Task<QuotationResponse> RespondAsync(Guid vendorUserId, Guid quotationId, RespondToQuotationRequest request);
}
