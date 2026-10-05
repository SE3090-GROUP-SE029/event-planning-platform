using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class PlanGenerationJobConfiguration : IEntityTypeConfiguration<PlanGenerationJob>
{
    public void Configure(EntityTypeBuilder<PlanGenerationJob> builder)
    {
        builder.ToTable("PlanGenerationJobs");

        builder.HasKey(job => job.Id);
        builder.Property(job => job.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();
        builder.Property(job => job.RequestId)
            .HasMaxLength(128)
            .IsRequired();
        builder.Property(job => job.FailureMessage)
            .HasMaxLength(1000);

        builder.HasIndex(job => job.EventId)
            .IsUnique()
            .HasFilter("\"Status\" IN ('Queued', 'Processing')");
        builder.HasIndex(job => new { job.Status, job.CreatedAt });

        builder.HasOne(job => job.Event)
            .WithMany()
            .HasForeignKey(job => job.EventId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(job => job.RequestedBy)
            .WithMany()
            .HasForeignKey(job => job.RequestedById)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EventPlanDraft>()
            .WithOne()
            .HasForeignKey<PlanGenerationJob>(job => job.PlanId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
