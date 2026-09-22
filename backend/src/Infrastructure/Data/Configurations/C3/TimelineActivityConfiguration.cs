using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.C3;

public class TimelineActivityConfiguration : IEntityTypeConfiguration<TimelineActivity>
{
    public void Configure(EntityTypeBuilder<TimelineActivity> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasMaxLength(50);

        // Foreign Key relationship to EventSchedule
        builder.HasOne(a => a.Schedule)
            .WithMany(s => s.Activities)
            .HasForeignKey(a => a.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        // PostgreSQL check constraint ensuring EndTime is strictly after StartTime
        builder.ToTable(t => 
            t.HasCheckConstraint("CK_TimelineActivity_ValidTimeRange", "\"EndTime\" > \"StartTime\""));
    }
}