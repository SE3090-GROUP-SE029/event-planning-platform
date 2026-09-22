using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.C2;

public class VendorGalleryImageConfiguration : IEntityTypeConfiguration<VendorGalleryImage>
{
    public void Configure(EntityTypeBuilder<VendorGalleryImage> builder)
    {
        builder.ToTable("VendorGalleryImages");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.VendorId).IsRequired();
        builder.Property(i => i.ImageUrl).IsRequired().HasMaxLength(500);
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.HasIndex(i => i.VendorId);

        builder.HasOne<Vendor>()
            .WithMany()
            .HasForeignKey(i => i.VendorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
