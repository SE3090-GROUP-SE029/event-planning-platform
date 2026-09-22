using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class VendorRepository : IVendorRepository
{
    private readonly AppDbContext _db;

    public VendorRepository(AppDbContext db) => _db = db;

    public Task<Vendor?> GetByUserIdAsync(Guid userId) =>
        _db.Vendors.FirstOrDefaultAsync(v => v.UserId == userId);

    public Task<Vendor?> GetByIdAsync(Guid id) =>
        _db.Vendors.FirstOrDefaultAsync(v => v.Id == id);

    public async Task AddAsync(Vendor vendor) => await _db.Vendors.AddAsync(vendor);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
