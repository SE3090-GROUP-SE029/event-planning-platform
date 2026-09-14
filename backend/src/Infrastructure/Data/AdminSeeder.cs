using Domain.Entities;
using Domain.Enums;
using Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data;

public static class AdminSeeder
{
    public static readonly Guid AdminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static async Task SeedAdminAsync(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger logger)
    {
        var adminEmail = configuration["AdminSeed:Email"] 
            ?? configuration["ADMIN_EMAIL"] 
            ?? "admin@planit.com";

        var adminPassword = configuration["AdminSeed:Password"] 
            ?? configuration["ADMIN_PASSWORD"];

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("Admin user seeding skipped: No password provided in configuration ('AdminSeed:Password' or 'ADMIN_PASSWORD').");
            return;
        }

        var normalizedEmail = adminEmail.Trim().ToLowerInvariant();

        // Ensure roles exist in the database (particularly when running with in-memory DB or fresh DB)
        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Id == AdminRoleId)
            ?? await db.Roles.FirstOrDefaultAsync(r => r.RoleName == RoleName.ADMIN);

        if (adminRole == null)
        {
            logger.LogError("Admin role ({RoleId}) was not found in the database. Ensure database migrations and role seeding have run.", AdminRoleId);
            return;
        }

        // Check if admin user already exists
        var existingUser = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (existingUser != null)
        {
            // Verify if ADMIN role is assigned, and assign it if missing
            if (!existingUser.UserRoles.Any(ur => ur.RoleId == adminRole.Id))
            {
                var newRoleAssignment = new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = existingUser.Id,
                    RoleId = adminRole.Id,
                    AssignedAt = DateTime.UtcNow
                };
                existingUser.UserRoles.Add(newRoleAssignment);
                await db.UserRoles.AddAsync(newRoleAssignment);
                await db.SaveChangesAsync();
                logger.LogInformation("Assigned ADMIN role to existing user '{Email}'.", normalizedEmail);
            }
            else
            {
                logger.LogInformation("Admin user '{Email}' already exists with ADMIN role. Skipping creation.", normalizedEmail);
            }
            return;
        }

        var firstName = configuration["AdminSeed:FirstName"] ?? "System";
        var lastName = configuration["AdminSeed:LastName"] ?? "Administrator";

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(adminPassword),
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = adminUser.Id,
            RoleId = adminRole.Id,
            AssignedAt = DateTime.UtcNow
        };

        adminUser.UserRoles.Add(userRole);

        await db.Users.AddAsync(adminUser);
        await db.UserRoles.AddAsync(userRole);
        await db.SaveChangesAsync();

        logger.LogInformation("Admin user '{Email}' successfully seeded with ADMIN role.", normalizedEmail);
    }
}
