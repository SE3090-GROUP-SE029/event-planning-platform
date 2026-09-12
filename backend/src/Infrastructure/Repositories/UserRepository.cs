using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db; 
    public Task<User?> GetByEmailAsync(string email) => _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Email == email);
    public Task<User?> GetByIdWithRoleAsync(Guid id) => _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Id == id);
    public Task<Role?> GetRoleByNameAsync(RoleName roleName) => _db.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
    public async Task AddAsync(User user) => await _db.Users.AddAsync(user);
    public async Task SaveChangesAsync() => _db.SaveChangesAsync();
}