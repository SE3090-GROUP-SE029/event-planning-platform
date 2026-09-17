using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.C2;

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.ToTable("Vendors");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.UserId).IsRequired();
        builder.HasIndex(v => v.UserId).IsUnique();
        builder.Property(v => v.BusinessName).IsRequired().HasMaxLength(200);
        builder.Property(v => v.Category).IsRequired();
        builder.Property(v => v.ContactEmail).IsRequired().HasMaxLength(256);
        builder.Property(v => v.ContactPhone).IsRequired().HasMaxLength(50);
        builder.Property(v => v.Description).HasMaxLength(2000);
        builder.Property(v => v.Status).IsRequired();
        builder.Property(v => v.CreatedAt).IsRequired();
    }
}
