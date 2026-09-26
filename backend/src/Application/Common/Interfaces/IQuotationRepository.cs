using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IQuotationRepository
{
    Task<Quotation?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Quotation>> ListByRequesterAsync(Guid requestedByUserId);
    Task<IReadOnlyList<Quotation>> ListByVendorIdAsync(Guid vendorId);
    Task AddAsync(Quotation quotation);
    Task SaveChangesAsync();
}
