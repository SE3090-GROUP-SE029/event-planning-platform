using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class VendorGalleryImageRepository : IVendorGalleryImageRepository
{
    private readonly AppDbContext _db;

    public VendorGalleryImageRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<VendorGalleryImage>> ListByVendorIdAsync(Guid vendorId) =>
        await _db.VendorGalleryImages
            .Where(i => i.VendorId == vendorId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

    public Task<int> CountByVendorIdAsync(Guid vendorId) =>
        _db.VendorGalleryImages.CountAsync(i => i.VendorId == vendorId);

    public Task<VendorGalleryImage?> GetByIdAsync(Guid id) =>
        _db.VendorGalleryImages.FirstOrDefaultAsync(i => i.Id == id);

    public async Task AddAsync(VendorGalleryImage image) =>
        await _db.VendorGalleryImages.AddAsync(image);

    public void Remove(VendorGalleryImage image) =>
        _db.VendorGalleryImages.Remove(image);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
