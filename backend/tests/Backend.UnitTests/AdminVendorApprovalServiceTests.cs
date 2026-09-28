using Application.Services.Admin;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Backend.UnitTests;

public class AdminVendorApprovalServiceTests
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

    [Fact]
    public async Task ApproveAsync_ApprovesPendingVendorAndSetsUpdatedAt()
    {
        using var db = CreateDb();
        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BusinessName = "Green Leaf Catering",
            Category = BusinessCategory.CATERING,
            ContactEmail = "hello@greenleaf.test",
            ContactPhone = "0771234567",
            Address = "12 Flower Road, Colombo",
            Status = VendorStatus.PENDING,
            CreatedAt = DateTime.UtcNow
        };
        db.Vendors.Add(vendor);
        await db.SaveChangesAsync();
        var service = new AdminVendorApprovalService(new VendorRepository(db));

        await service.ApproveAsync(vendor.Id);

        Assert.Equal(VendorStatus.APPROVED, vendor.Status);
        Assert.NotNull(vendor.UpdatedAt);
    }

    [Fact]
    public async Task ApproveAsync_ThrowsWhenVendorDoesNotExist()
    {
        using var db = CreateDb();
        var service = new AdminVendorApprovalService(new VendorRepository(db));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ApproveAsync(Guid.NewGuid()));
    }

    [Theory]
    [InlineData(VendorStatus.APPROVED)]
    [InlineData(VendorStatus.SUSPEND)]
    public async Task ApproveAsync_ThrowsWhenVendorIsNotPending(VendorStatus status)
    {
        using var db = CreateDb();
        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BusinessName = "Green Leaf Catering",
            Category = BusinessCategory.CATERING,
            ContactEmail = "hello@greenleaf.test",
            ContactPhone = "0771234567",
            Address = "12 Flower Road, Colombo",
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
        db.Vendors.Add(vendor);
        await db.SaveChangesAsync();
        var service = new AdminVendorApprovalService(new VendorRepository(db));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(vendor.Id));
        Assert.Equal(status, vendor.Status);
    }
}
