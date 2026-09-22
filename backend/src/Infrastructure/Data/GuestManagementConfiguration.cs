using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public static class GuestManagementConfiguration
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GuestAiReview>(entity =>
        {
            entity.ToTable("GuestAiReviews", table =>
            {
                table.HasCheckConstraint("CK_GuestAiReviews_Status", "\"Status\" IN ('PENDING', 'PROCESSING', 'COMPLETED', 'FAILED')");
                table.HasCheckConstraint("CK_GuestAiReviews_Recommendation", "\"Recommendation\" IS NULL OR \"Recommendation\" IN ('ELIGIBLE', 'REVIEW', 'REJECTED')");
                table.HasCheckConstraint("CK_GuestAiReviews_Decision", "\"Decision\" IS NULL OR \"Decision\" IN ('ACCEPTED', 'REJECTED')");
                table.HasCheckConstraint("CK_GuestAiReviews_Confidence", "\"Confidence\" IS NULL OR (\"Confidence\" >= 0 AND \"Confidence\" <= 1)");
                table.HasCheckConstraint("CK_GuestAiReviews_Result", "(\"Status\" = 'COMPLETED' AND (\"Decision\" IS NOT NULL OR \"Recommendation\" IS NOT NULL) AND \"Confidence\" IS NOT NULL AND \"AnalyzedAt\" IS NOT NULL AND cardinality(\"Reasons\") > 0 AND \"Model\" IS NOT NULL AND \"PromptVersion\" IS NOT NULL) OR (\"Status\" <> 'COMPLETED' AND \"Decision\" IS NULL AND \"Confidence\" IS NULL AND \"AnalyzedAt\" IS NULL)");
                table.HasCheckConstraint("CK_GuestAiReviews_Lease", "\"Status\" <> 'PROCESSING' OR (\"AttemptId\" IS NOT NULL AND \"LeaseExpiresAt\" IS NOT NULL)");
            });
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RegistrationSubmissionId).IsUnique();
            entity.HasIndex(e => new { e.Status, e.RequestedAt });
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Recommendation).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Decision).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Model).HasMaxLength(120);
            entity.Property(e => e.PromptVersion).HasMaxLength(40);
            entity.Property(e => e.FailureCode).HasMaxLength(40);
            entity.HasOne(e => e.RegistrationSubmission).WithOne().HasForeignKey<GuestAiReview>(e => e.RegistrationSubmissionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events", table => table.HasCheckConstraint("CK_Events_Dates", "\"EventEndDate\" > \"EventStartDate\""));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedByUserId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.EventName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.RequirementNotes).HasMaxLength(4000);
            entity.Property(e => e.PreferredLocation).HasMaxLength(500);
        });

        modelBuilder.Entity<RegistrationForm>(entity =>
        {
            entity.ToTable("RegistrationForms", table =>
            {
                table.HasCheckConstraint("CK_RegistrationForms_Period", "\"OpensAt\" < \"ClosesAt\"");
                table.HasCheckConstraint("CK_RegistrationForms_SeatLimit", "\"SeatLimit\" > 0");
                table.HasCheckConstraint("CK_RegistrationForms_Publication", "(\"Status\" = 'DRAFT' AND \"PublicId\" IS NULL AND \"PublishedAt\" IS NULL) OR (\"Status\" = 'PUBLISHED' AND \"PublicId\" IS NOT NULL AND \"PublishedAt\" IS NOT NULL)");
            });
            entity.HasKey(e => e.Id);
            entity.HasAlternateKey(e => new { e.Id, e.EventId });
            entity.HasIndex(e => e.EventId).IsUnique();
            entity.HasIndex(e => e.PublicId).IsUnique();
            entity.Property(e => e.PublicId).HasMaxLength(43);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(e => e.Event).WithMany().HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Guest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasAlternateKey(e => new { e.Id, e.EventId });
            entity.HasIndex(e => new { e.EventId, e.NormalizedEmail }).IsUnique();
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.EmailAddress).IsRequired().HasMaxLength(254);
            entity.Property(e => e.NormalizedEmail).IsRequired().HasMaxLength(254);
            entity.Property(e => e.Organisation).HasMaxLength(200);
            entity.Property(e => e.PhoneNumber).HasMaxLength(40);
            entity.HasOne<Event>().WithMany().HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RegistrationSubmission>(entity =>
        {
            entity.ToTable("RegistrationSubmissions", table => table.HasCheckConstraint("CK_RegistrationSubmissions_Status",
                "\"Status\" IN ('CONFIRMED', 'WAITING_LIST', 'CANCELLED', 'PENDING_AI', 'REJECTED')"));
            entity.HasKey(e => e.Id);
            entity.HasAlternateKey(e => new { e.Id, e.RegistrationFormId });
            entity.Property(e => e.RejectionDeliveryStatus).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(e => e.GuestId).IsUnique();
            entity.HasIndex(e => e.PublicReference).IsUnique();
            entity.HasIndex(e => new { e.EventId, e.Status, e.RegisteredAt, e.Id });
            entity.Property(e => e.PublicReference).IsRequired().HasMaxLength(43);
            entity.Property(e => e.StatusSecretHash).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(e => e.RegistrationForm).WithMany().HasForeignKey(e => new { e.RegistrationFormId, e.EventId })
                .HasPrincipalKey(e => new { e.Id, e.EventId }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Guest).WithMany().HasForeignKey(e => new { e.GuestId, e.EventId })
                .HasPrincipalKey(e => new { e.Id, e.EventId }).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RegistrationQuestion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasAlternateKey(e => new { e.Id, e.RegistrationFormId });
            entity.Property(e => e.Question).IsRequired().HasMaxLength(500);
            entity.HasIndex(e => new { e.RegistrationFormId, e.DisplayOrder }).IsUnique().HasFilter("\"IsSelected\" = TRUE");
            entity.HasOne(e => e.RegistrationForm).WithMany(e => e.Questions).HasForeignKey(e => e.RegistrationFormId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RegistrationAnswer>(entity =>
        {
            entity.HasKey(e => new { e.RegistrationSubmissionId, e.RegistrationQuestionId });
            entity.Property(e => e.Answer).IsRequired().HasMaxLength(4000);
            entity.HasOne(e => e.RegistrationSubmission).WithMany(e => e.Answers)
                .HasForeignKey(e => new { e.RegistrationSubmissionId, e.RegistrationFormId })
                .HasPrincipalKey(e => new { e.Id, e.RegistrationFormId }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.RegistrationQuestion).WithMany()
                .HasForeignKey(e => new { e.RegistrationQuestionId, e.RegistrationFormId })
                .HasPrincipalKey(e => new { e.Id, e.RegistrationFormId }).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.Token).IsRequired().HasMaxLength(43);
            entity.Property(e => e.DeliveryStatus).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.RsvpStatus).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(e => e.RegistrationSubmission).WithOne(e => e.Invitation)
                .HasForeignKey<Invitation>(e => e.RegistrationSubmissionId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
