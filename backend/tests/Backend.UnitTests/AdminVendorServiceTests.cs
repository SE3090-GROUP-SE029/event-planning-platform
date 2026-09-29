using Application.Dtos.Vendors;
using Application.Services.Vendors;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Backend.UnitTests;

public class AdminVendorServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task<Vendor> SeedVendorAsync(
        AppDbContext db,
        string businessName,
        VendorStatus status,
        BusinessCategory category = BusinessCategory.PHOTOGRAPHY)
    {
        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BusinessName = businessName,
            Category = category,
            ContactEmail = $"{businessName.Replace(" ", "").ToLowerInvariant()}@test.local",
            ContactPhone = "0770000000",
            Address = "1 Test Street",
            Description = "Test vendor",
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        db.Vendors.Add(vendor);
        await db.SaveChangesAsync();
        return vendor;
    }

    [Fact]
    public async Task ListAsync_FiltersByStatus()
    {
        using var db = CreateDb();
        await SeedVendorAsync(db, "Pending Studio", VendorStatus.PENDING);
        await SeedVendorAsync(db, "Approved Studio", VendorStatus.APPROVED);
        await SeedVendorAsync(db, "Suspended Studio", VendorStatus.SUSPEND);
        var service = new AdminVendorService(new VendorRepository(db));

        var pending = await service.ListAsync(new AdminVendorQuery { Status = VendorStatus.PENDING });
        var approved = await service.ListAsync(new AdminVendorQuery { Status = VendorStatus.APPROVED });
        var suspended = await service.ListAsync(new AdminVendorQuery { Status = VendorStatus.SUSPEND });

        Assert.Single(pending.Items);
        Assert.Equal("Pending Studio", pending.Items[0].BusinessName);
        Assert.Single(approved.Items);
        Assert.Equal("Approved Studio", approved.Items[0].BusinessName);
        Assert.Single(suspended.Items);
        Assert.Equal("Suspended Studio", suspended.Items[0].BusinessName);
    }

    [Fact]
    public async Task ApproveAsync_MovesPendingToApproved()
    {
        using var db = CreateDb();
        var vendor = await SeedVendorAsync(db, "Pending Studio", VendorStatus.PENDING);
        var service = new AdminVendorService(new VendorRepository(db));

        var result = await service.ApproveAsync(vendor.Id);

        Assert.Equal(nameof(VendorStatus.APPROVED), result.Status);
        Assert.Equal(VendorStatus.APPROVED, (await db.Vendors.FindAsync(vendor.Id))!.Status);
    }

    [Fact]
    public async Task ApproveAsync_Throws_WhenNotPending()
    {
        using var db = CreateDb();
        var vendor = await SeedVendorAsync(db, "Approved Studio", VendorStatus.APPROVED);
        var service = new AdminVendorService(new VendorRepository(db));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(vendor.Id));
        Assert.Contains("PENDING", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SuspendAsync_MovesApprovedToSuspend()
    {
        using var db = CreateDb();
        var vendor = await SeedVendorAsync(db, "Approved Studio", VendorStatus.APPROVED);
        var service = new AdminVendorService(new VendorRepository(db));

        var result = await service.SuspendAsync(vendor.Id);

        Assert.Equal(nameof(VendorStatus.SUSPEND), result.Status);
        Assert.Equal(VendorStatus.SUSPEND, (await db.Vendors.FindAsync(vendor.Id))!.Status);
    }

    [Fact]
    public async Task SuspendAsync_Throws_WhenNotApproved()
    {
        using var db = CreateDb();
        var vendor = await SeedVendorAsync(db, "Pending Studio", VendorStatus.PENDING);
        var service = new AdminVendorService(new VendorRepository(db));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SuspendAsync(vendor.Id));
        Assert.Contains("APPROVED", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RestoreAsync_MovesSuspendToApproved()
    {
        using var db = CreateDb();
        var vendor = await SeedVendorAsync(db, "Suspended Studio", VendorStatus.SUSPEND);
        var service = new AdminVendorService(new VendorRepository(db));

        var result = await service.RestoreAsync(vendor.Id);

        Assert.Equal(nameof(VendorStatus.APPROVED), result.Status);
        Assert.Equal(VendorStatus.APPROVED, (await db.Vendors.FindAsync(vendor.Id))!.Status);
    }

    [Fact]
    public async Task RestoreAsync_Throws_WhenNotSuspended()
    {
        using var db = CreateDb();
        var vendor = await SeedVendorAsync(db, "Pending Studio", VendorStatus.PENDING);
        var service = new AdminVendorService(new VendorRepository(db));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RestoreAsync(vendor.Id));
        Assert.Contains("SUSPEND", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetByIdAsync_Throws_WhenMissing()
    {
        using var db = CreateDb();
        var service = new AdminVendorService(new VendorRepository(db));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task SuspendAsync_DoesNotTouchBookingsTable()
    {
        using var db = CreateDb();
        var vendor = await SeedVendorAsync(db, "Approved Studio", VendorStatus.APPROVED);
        db.Bookings.Add(new Booking
        {
            Id = Guid.NewGuid(),
            QuotationId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            VendorId = vendor.Id,
            VendorServiceId = Guid.NewGuid(),
            RequestedByUserId = Guid.NewGuid(),
            StartDateTime = DateTime.UtcNow.AddDays(1),
            EndDateTime = DateTime.UtcNow.AddDays(2),
            AgreedPrice = 1000m,
            Status = BookingStatus.CONFIRMED,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new AdminVendorService(new VendorRepository(db));
        await service.SuspendAsync(vendor.Id);

        var booking = Assert.Single(db.Bookings);
        Assert.Equal(BookingStatus.CONFIRMED, booking.Status);
        Assert.Null(booking.CancelledAt);
    }
}
