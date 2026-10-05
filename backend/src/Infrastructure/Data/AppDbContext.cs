using System.Reflection;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public override int SaveChanges()
    {
        NormalizeTrackedDateTimes();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        NormalizeTrackedDateTimes();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        NormalizeTrackedDateTimes();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        NormalizeTrackedDateTimes();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // C4 Guest Management DbSets
    public DbSet<TestMessage> TestMessages { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<RegistrationForm> RegistrationForms { get; set; }
    public DbSet<Guest> Guests { get; set; }
    public DbSet<RegistrationSubmission> RegistrationSubmissions { get; set; }
    public DbSet<Invitation> Invitations { get; set; }
    public DbSet<GuestAiReview> GuestAiReviews { get; set; }
    public DbSet<RegistrationLinkEmailJob> RegistrationLinkEmailJobs => Set<RegistrationLinkEmailJob>();
    public DbSet<GuestCheckIn> GuestCheckIns { get; set; }
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
    public DbSet<VendorRating> VendorRatings => Set<VendorRating>();
    public DbSet<EventPlanDraft> EventPlanDrafts => Set<EventPlanDraft>();
    public DbSet<PlanGenerationJob> PlanGenerationJobs => Set<PlanGenerationJob>();
    public DbSet<VendorRecommendationRun> VendorRecommendationRuns => Set<VendorRecommendationRun>();
    public DbSet<VendorRecommendationItem> VendorRecommendationItems => Set<VendorRecommendationItem>();

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

    private void NormalizeTrackedDateTimes()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                foreach (var property in entry.Properties)
                {
                    if (property.CurrentValue is DateTime dateTime)
                    {
                        property.CurrentValue = Application.Common.UtcDateTime.Normalize(dateTime);
                    }
                    else if (property.CurrentValue is DateTimeOffset dateTimeOffset)
                    {
                        property.CurrentValue = Application.Common.UtcDateTime.Normalize(dateTimeOffset);
                    }
                }
            }
        }
    }
}
