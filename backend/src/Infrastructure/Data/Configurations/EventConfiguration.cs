using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_Events_GuestCount_Positive", "\"GuestCount\" > 0");
            tableBuilder.HasCheckConstraint("CK_Events_Budget_NonNegative", "\"Budget\" >= 0");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.OwnerId).IsRequired();
        builder.HasIndex(e => e.OwnerId);
        builder.Property(e => e.EventType).IsRequired();
        builder.Property(e => e.GuestCount).IsRequired();
        builder.Property(e => e.Budget).IsRequired().HasPrecision(18, 2);
        builder.Property(e => e.PreferredVenue).HasMaxLength(500);
        builder.Property(e => e.PreferredDate).IsRequired();
        builder.Property(e => e.EventDuration).IsRequired();
        builder.Property(e => e.Requirements).HasMaxLength(4000);
        builder.Property(e => e.Status).IsRequired().HasDefaultValue(EventStatus.DRAFT);
        builder.Property(e => e.CreatedAt).IsRequired();
    }
}
