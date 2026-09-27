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

    public DbSet<VendorOffering> VendorOfferings => Set<VendorOffering>();
    public DbSet<VendorGalleryImage> VendorGalleryImages => Set<VendorGalleryImage>();
    public DbSet<VendorAvailability> VendorAvailabilities => Set<VendorAvailability>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<EventPlanDraft> EventPlanDrafts => Set<EventPlanDraft>();

    // Dev Scheduling DbSets
    public DbSet<EventSchedule> EventSchedules => Set<EventSchedule>();
    public DbSet<TimelineActivity> TimelineActivities => Set<TimelineActivity>();
    public DbSet<ScheduleConflict> ScheduleConflicts => Set<ScheduleConflict>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        GuestManagementConfiguration.Configure(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
