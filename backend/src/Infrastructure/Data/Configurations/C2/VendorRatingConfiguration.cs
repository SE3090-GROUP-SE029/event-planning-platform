using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.C2;

public class VendorRatingConfiguration : IEntityTypeConfiguration<VendorRating>
{
    public void Configure(EntityTypeBuilder<VendorRating> builder)
    {
        builder.ToTable("VendorRatings", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_VendorRatings_Rating_Range",
                "\"Rating\" >= 1 AND \"Rating\" <= 5");
        });

        builder.HasKey(r => r.Id);
        builder.Property(r => r.BookingId).IsRequired();
        builder.Property(r => r.VendorId).IsRequired();
        builder.Property(r => r.ReviewerUserId).IsRequired();
        builder.Property(r => r.Rating).IsRequired();
        builder.Property(r => r.Comment).HasMaxLength(2000);
        builder.Property(r => r.CreatedAt).IsRequired();

        builder.HasIndex(r => r.BookingId).IsUnique();
        builder.HasIndex(r => r.VendorId);
        builder.HasIndex(r => r.ReviewerUserId);

        builder.HasOne<Booking>()
            .WithMany()
            .HasForeignKey(r => r.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Vendor>()
            .WithMany()
            .HasForeignKey(r => r.VendorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
