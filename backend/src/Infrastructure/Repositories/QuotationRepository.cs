using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class QuotationRepository : IQuotationRepository
{
    private readonly AppDbContext _db;

    public QuotationRepository(AppDbContext db) => _db = db;

    public Task<Quotation?> GetByIdAsync(Guid id) =>
        _db.Quotations.FirstOrDefaultAsync(q => q.Id == id);

    public async Task<IReadOnlyList<Quotation>> ListByRequesterAsync(Guid requestedByUserId) =>
        await _db.Quotations
            .Where(q => q.RequestedByUserId == requestedByUserId)
            .OrderByDescending(q => q.RequestedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<Quotation>> ListByVendorIdAsync(Guid vendorId) =>
        await _db.Quotations
            .Where(q => q.VendorId == vendorId)
            .OrderByDescending(q => q.RequestedAt)
            .ToListAsync();

    public async Task AddAsync(Quotation quotation) =>
        await _db.Quotations.AddAsync(quotation);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
