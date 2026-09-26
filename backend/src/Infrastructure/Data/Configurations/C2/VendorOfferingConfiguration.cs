using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.C2;

public class VendorOfferingConfiguration : IEntityTypeConfiguration<VendorOffering>
{
    public void Configure(EntityTypeBuilder<VendorOffering> builder)
    {
        builder.ToTable("VendorServices");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.VendorId).IsRequired();
        builder.Property(o => o.ServiceName).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Description).HasMaxLength(2000);
        builder.Property(o => o.Price).HasPrecision(18, 2);
        builder.Property(o => o.PricingType);
        builder.Property(o => o.CreatedAt).IsRequired();

        builder.HasIndex(o => o.VendorId);

        builder.HasOne<Vendor>()
            .WithMany()
            .HasForeignKey(o => o.VendorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
