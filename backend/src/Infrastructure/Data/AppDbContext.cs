using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TestMessage> TestMessages { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<RegistrationForm> RegistrationForms { get; set; }
    public DbSet<Guest> Guests { get; set; }
    public DbSet<RegistrationSubmission> RegistrationSubmissions { get; set; }
    public DbSet<Invitation> Invitations { get; set; }
    public DbSet<GuestAiReview> GuestAiReviews { get; set; }
    public DbSet<RegistrationQuestion> RegistrationQuestions { get; set; }
    public DbSet<RegistrationAnswer> RegistrationAnswers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        GuestManagementConfiguration.Configure(modelBuilder);

        modelBuilder.Entity<TestMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
        });
    }
}
