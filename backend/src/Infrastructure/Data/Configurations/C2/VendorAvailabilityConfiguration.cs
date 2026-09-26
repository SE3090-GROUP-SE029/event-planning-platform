using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.C2;

public class VendorAvailabilityConfiguration : IEntityTypeConfiguration<VendorAvailability>
{
    public void Configure(EntityTypeBuilder<VendorAvailability> builder)
    {
        builder.ToTable("VendorAvailability");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.VendorId).IsRequired();
        builder.Property(a => a.StartDateTime).IsRequired();
        builder.Property(a => a.EndDateTime).IsRequired();
        builder.Property(a => a.IsAvailable).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();

        builder.HasIndex(a => new { a.VendorId, a.StartDateTime });

        builder.HasOne<Vendor>()
            .WithMany()
            .HasForeignKey(a => a.VendorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
