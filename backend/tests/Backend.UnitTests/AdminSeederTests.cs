using Domain.Entities;
using Domain.Enums;
using Infrastructure.Auth;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Backend.UnitTests;

public class AdminSeederTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task SeedAdminAsync_SeedsAdminUserSuccessfully_WhenUserDoesNotExist()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var passwordHasher = new PasswordHasher();
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "AdminSeed:Email", "admin@planit.com" },
            { "AdminSeed:Password", "SecureAdminPass123!" },
            { "AdminSeed:FirstName", "Super" },
            { "AdminSeed:LastName", "Admin" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var logger = NullLogger.Instance;

        // Act
        await AdminSeeder.SeedAdminAsync(db, passwordHasher, configuration, logger);

        // Assert
        var user = await db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == "admin@planit.com");

        Assert.NotNull(user);
        Assert.Equal("admin@planit.com", user.Email);
        Assert.Equal("Super", user.FirstName);
        Assert.Equal("Admin", user.LastName);
        Assert.True(user.IsActive);
        Assert.True(passwordHasher.Verify("SecureAdminPass123!", user.PasswordHash));

        Assert.Single(user.UserRoles);
        var userRole = user.UserRoles.First();
        Assert.Equal(AdminSeeder.AdminRoleId, userRole.RoleId);
        Assert.Equal(RoleName.ADMIN, userRole.Role.RoleName);
    }

    [Fact]
    public async Task SeedAdminAsync_IsIdempotent_WhenRunMultipleTimes()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var passwordHasher = new PasswordHasher();
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "AdminSeed:Email", "admin@planit.com" },
            { "AdminSeed:Password", "SecureAdminPass123!" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var logger = NullLogger.Instance;

        // Act - Call twice
        await AdminSeeder.SeedAdminAsync(db, passwordHasher, configuration, logger);
        await AdminSeeder.SeedAdminAsync(db, passwordHasher, configuration, logger);

        // Assert
        var users = await db.Users.Where(u => u.Email == "admin@planit.com").ToListAsync();
        Assert.Single(users);

        var userRoles = await db.UserRoles.Where(ur => ur.UserId == users[0].Id).ToListAsync();
        Assert.Single(userRoles);
    }

    [Fact]
    public async Task SeedAdminAsync_SkipsSeeding_WhenPasswordIsNotConfigured()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var passwordHasher = new PasswordHasher();
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "AdminSeed:Email", "admin@planit.com" }
            // No password provided
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var logger = NullLogger.Instance;

        // Act
        await AdminSeeder.SeedAdminAsync(db, passwordHasher, configuration, logger);

        // Assert
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@planit.com");
        Assert.Null(user);
    }

    [Fact]
    public async Task SeedAdminAsync_AssignsRole_WhenUserExistsWithoutAdminRole()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var passwordHasher = new PasswordHasher();

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@planit.com",
            PasswordHash = passwordHasher.Hash("ExistingPassword123!"),
            FirstName = "Existing",
            LastName = "User",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        await db.Users.AddAsync(existingUser);
        await db.SaveChangesAsync();

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "AdminSeed:Email", "admin@planit.com" },
            { "AdminSeed:Password", "NewPasswordIgnored!" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var logger = NullLogger.Instance;

        // Act
        await AdminSeeder.SeedAdminAsync(db, passwordHasher, configuration, logger);

        // Assert
        var user = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == "admin@planit.com");

        Assert.NotNull(user);
        Assert.Single(user.UserRoles);
        Assert.Equal(AdminSeeder.AdminRoleId, user.UserRoles.First().RoleId);
    }
}
