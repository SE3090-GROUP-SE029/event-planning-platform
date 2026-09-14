using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdWithRoleAsync(Guid id);
    Task<Role?> GetRoleByNameAsync(RoleName roleName);
    Task AddAsync(User user);
    Task SaveChangesAsync();
}