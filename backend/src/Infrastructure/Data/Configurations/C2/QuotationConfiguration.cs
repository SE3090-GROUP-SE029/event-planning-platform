using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.C2;

public class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
{
    public void Configure(EntityTypeBuilder<Quotation> builder)
    {
        builder.ToTable("Quotations", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_Quotations_QuotedPrice_NonNegative",
                "\"QuotedPrice\" IS NULL OR \"QuotedPrice\" >= 0");
            tableBuilder.HasCheckConstraint(
                "CK_Quotations_DateRange",
                "\"RequestedStartDateTime\" < \"RequestedEndDateTime\"");
        });

        builder.HasKey(q => q.Id);
        builder.Property(q => q.EventId).IsRequired();
        builder.Property(q => q.VendorId).IsRequired();
        builder.Property(q => q.VendorServiceId).IsRequired();
        builder.Property(q => q.RequestedByUserId).IsRequired();
        builder.Property(q => q.RequestedStartDateTime).IsRequired();
        builder.Property(q => q.RequestedEndDateTime).IsRequired();
        builder.Property(q => q.CustomerMessage).HasMaxLength(2000);
        builder.Property(q => q.QuotedPrice).HasPrecision(18, 2);
        builder.Property(q => q.VendorTerms).HasMaxLength(4000);
        builder.Property(q => q.Status).IsRequired().HasDefaultValue(QuotationStatus.REQUESTED);
        builder.Property(q => q.RequestedAt).IsRequired();

        builder.HasIndex(q => q.VendorId);
        builder.HasIndex(q => q.RequestedByUserId);
        builder.HasIndex(q => q.EventId);
        builder.HasIndex(q => q.Status);

        // C1 Event reference without inverse navigation on Event.
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(q => q.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Vendor>()
            .WithMany()
            .HasForeignKey(q => q.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<VendorOffering>()
            .WithMany()
            .HasForeignKey(q => q.VendorServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
