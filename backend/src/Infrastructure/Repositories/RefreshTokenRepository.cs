using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;
    public RefreshTokenRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(RefreshToken token) => await _db.RefreshTokens.AddAsync(token);

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash) => _db.RefreshTokens
        .Include(rt => rt.User)
        .ThenInclude(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
        .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
