using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.C2;

public class VendorRecommendationItemConfiguration : IEntityTypeConfiguration<VendorRecommendationItem>
{
    public void Configure(EntityTypeBuilder<VendorRecommendationItem> builder)
    {
        builder.ToTable("VendorRecommendationItems", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_VendorRecommendationItems_Score_Range",
                "\"Score\" >= 0 AND \"Score\" <= 100");
            tableBuilder.HasCheckConstraint(
                "CK_VendorRecommendationItems_Rank_Positive",
                "\"Rank\" >= 1");
        });

        builder.HasKey(i => i.Id);
        builder.Property(i => i.RunId).IsRequired();
        builder.Property(i => i.VendorId).IsRequired();
        builder.Property(i => i.Rank).IsRequired();
        builder.Property(i => i.Score).IsRequired();
        builder.Property(i => i.Reason).IsRequired().HasMaxLength(1000);
        builder.Property(i => i.BusinessName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Category).IsRequired().HasMaxLength(50);
        builder.Property(i => i.ServiceName).HasMaxLength(200);
        builder.Property(i => i.Price).HasPrecision(18, 2);
        builder.Property(i => i.PricingType).HasMaxLength(40);
        builder.Property(i => i.AverageRating).HasPrecision(3, 2);
        builder.Property(i => i.ReviewCount).IsRequired();
        builder.Property(i => i.AvailabilityMatch).IsRequired();

        builder.HasIndex(i => i.RunId);
        builder.HasIndex(i => i.VendorId);

        builder.HasOne<Vendor>()
            .WithMany()
            .HasForeignKey(i => i.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<VendorOffering>()
            .WithMany()
            .HasForeignKey(i => i.VendorServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
