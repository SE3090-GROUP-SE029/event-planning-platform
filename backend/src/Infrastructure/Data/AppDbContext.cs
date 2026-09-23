using System.Reflection;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users {get; set;} = default!;
    public DbSet<Role> Roles {get; set;} = default!;
    public DbSet<UserRole> UserRoles {get; set;} = default!;
    public DbSet<RefreshToken> RefreshTokens {get; set;} = default!;
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<VendorOffering> VendorOfferings => Set<VendorOffering>();
    public DbSet<VendorGalleryImage> VendorGalleryImages => Set<VendorGalleryImage>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventPlanDraft> EventPlanDrafts => Set<EventPlanDraft>();

    public DbSet<EventSchedule> EventSchedules => Set<EventSchedule>();
    public DbSet<TimelineActivity> TimelineActivities => Set<TimelineActivity>();
    public DbSet<ScheduleConflict> ScheduleConflicts => Set<ScheduleConflict>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}