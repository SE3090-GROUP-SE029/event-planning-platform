using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.C2;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_Bookings_AgreedPrice_NonNegative",
                "\"AgreedPrice\" >= 0");
            tableBuilder.HasCheckConstraint(
                "CK_Bookings_DateRange",
                "\"StartDateTime\" < \"EndDateTime\"");
        });

        builder.HasKey(b => b.Id);
        builder.Property(b => b.QuotationId).IsRequired();
        builder.Property(b => b.EventId).IsRequired();
        builder.Property(b => b.VendorId).IsRequired();
        builder.Property(b => b.VendorServiceId).IsRequired();
        builder.Property(b => b.RequestedByUserId).IsRequired();
        builder.Property(b => b.StartDateTime).IsRequired();
        builder.Property(b => b.EndDateTime).IsRequired();
        builder.Property(b => b.AgreedPrice).IsRequired().HasPrecision(18, 2);
        builder.Property(b => b.VendorTerms).HasMaxLength(4000);
        builder.Property(b => b.Status).IsRequired();
        builder.Property(b => b.CancellationReason).HasMaxLength(2000);
        builder.Property(b => b.CreatedAt).IsRequired();

        builder.HasIndex(b => b.QuotationId).IsUnique();
        builder.HasIndex(b => b.VendorId);
        builder.HasIndex(b => b.RequestedByUserId);
        builder.HasIndex(b => b.Status);
        builder.HasIndex(b => new { b.VendorId, b.StartDateTime, b.EndDateTime });

        builder.HasOne<Quotation>()
            .WithMany()
            .HasForeignKey(b => b.QuotationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(b => b.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Vendor>()
            .WithMany()
            .HasForeignKey(b => b.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<VendorOffering>()
            .WithMany()
            .HasForeignKey(b => b.VendorServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
