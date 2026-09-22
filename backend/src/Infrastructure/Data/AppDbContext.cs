using System.Reflection;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // C4 Guest Management DbSets
    public DbSet<TestMessage> TestMessages { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<RegistrationForm> RegistrationForms { get; set; }
    public DbSet<Guest> Guests { get; set; }
    public DbSet<RegistrationSubmission> RegistrationSubmissions { get; set; }
    public DbSet<Invitation> Invitations { get; set; }
    public DbSet<GuestAiReview> GuestAiReviews { get; set; }
    public DbSet<RegistrationQuestion> RegistrationQuestions { get; set; }
    public DbSet<RegistrationAnswer> RegistrationAnswers { get; set; }

    // Dev Authentication, Users & Vendors DbSets
    public DbSet<User> Users {get; set;} = default!;
    public DbSet<Role> Roles {get; set;} = default!;
    public DbSet<UserRole> UserRoles {get; set;} = default!;
    public DbSet<RefreshToken> RefreshTokens {get; set;} = default!;
    public DbSet<Vendor> Vendors => Set<Vendor>();

    // Dev Scheduling DbSets
    public DbSet<EventSchedule> EventSchedules => Set<EventSchedule>();
    public DbSet<TimelineActivity> TimelineActivities => Set<TimelineActivity>();
    public DbSet<ScheduleConflict> ScheduleConflicts => Set<ScheduleConflict>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Note: Event entity configuration will conflict here until C4 adaptation is complete.
        GuestManagementConfiguration.Configure(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
