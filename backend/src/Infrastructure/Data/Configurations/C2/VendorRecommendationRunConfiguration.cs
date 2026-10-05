using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.C2;

public class VendorRecommendationRunConfiguration : IEntityTypeConfiguration<VendorRecommendationRun>
{
    public void Configure(EntityTypeBuilder<VendorRecommendationRun> builder)
    {
        builder.ToTable("VendorRecommendationRuns");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.EventId).IsRequired();
        builder.Property(r => r.EventPlanDraftId).IsRequired();
        builder.Property(r => r.RequestedByUserId).IsRequired();
        builder.Property(r => r.CandidateCount).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();
        builder.Property(r => r.Status).HasMaxLength(16).IsRequired();
        builder.Property(r => r.Stage).HasMaxLength(64).IsRequired();
        builder.Property(r => r.FailureMessage).HasMaxLength(1000);
        builder.Property(r => r.SourceNote).HasMaxLength(500);

        builder.HasIndex(r => new { r.EventId, r.CreatedAt });
        builder.HasIndex(r => r.EventPlanDraftId);
        builder.HasIndex(r => r.EventId)
            .IsUnique()
            .HasFilter("\"Status\" IN ('Pending', 'Running')");

        builder.HasOne(r => r.Event)
            .WithMany()
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.EventPlanDraft)
            .WithMany()
            .HasForeignKey(r => r.EventPlanDraftId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Items)
            .WithOne(i => i.Run)
            .HasForeignKey(i => i.RunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
