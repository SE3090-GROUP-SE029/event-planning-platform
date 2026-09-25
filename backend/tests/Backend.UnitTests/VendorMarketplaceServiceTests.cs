using Application.Dtos.Vendors;
using Application.Services.Vendors;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Backend.UnitTests;

public class VendorMarketplaceServiceTests
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

    private static VendorMarketplaceService CreateService(AppDbContext db) =>
        new(
            new VendorMarketplaceRepository(db),
            new VendorOfferingRepository(db),
            new VendorGalleryImageRepository(db),
            new VendorAvailabilityRepository(db));

    private static async Task<Vendor> SeedVendorAsync(
        AppDbContext db,
        string businessName,
        BusinessCategory category,
        VendorStatus status,
        string? description = null)
    {
        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BusinessName = businessName,
            Category = category,
            ContactEmail = $"{Guid.NewGuid():N}@private.test",
            ContactPhone = "0770000000",
            Address = "12 Flower Road, Colombo",
            Description = description,
            ProfileImageUrl = "/uploads/vendors/logo.jpg",
            WebsiteUrl = "https://example.test",
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
        db.Vendors.Add(vendor);
        await db.SaveChangesAsync();
        return vendor;
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlyApprovedVendors()
    {
        using var db = CreateDb();
        var approved = await SeedVendorAsync(db, "KG Photography", BusinessCategory.PHOTOGRAPHY, VendorStatus.APPROVED, "Weddings");
        await SeedVendorAsync(db, "Pending Studio", BusinessCategory.PHOTOGRAPHY, VendorStatus.PENDING);
        await SeedVendorAsync(db, "Suspended Catering", BusinessCategory.CATERING, VendorStatus.SUSPEND);
        var service = CreateService(db);

        var result = await service.ListAsync(new VendorMarketplaceQuery());

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(approved.Id, result.Items[0].Id);
        Assert.Equal("KG Photography", result.Items[0].BusinessName);
    }

    [Fact]
    public async Task ListAsync_SearchFiltersByBusinessName()
    {
        using var db = CreateDb();
        await SeedVendorAsync(db, "KG Photography", BusinessCategory.PHOTOGRAPHY, VendorStatus.APPROVED);
        await SeedVendorAsync(db, "Green Leaf Catering", BusinessCategory.CATERING, VendorStatus.APPROVED);
        var service = CreateService(db);

        var result = await service.ListAsync(new VendorMarketplaceQuery { Search = "leaf" });

        Assert.Single(result.Items);
        Assert.Equal("Green Leaf Catering", result.Items[0].BusinessName);
    }

    [Fact]
    public async Task ListAsync_CategoryFilterWorks()
    {
        using var db = CreateDb();
        await SeedVendorAsync(db, "KG Photography", BusinessCategory.PHOTOGRAPHY, VendorStatus.APPROVED);
        await SeedVendorAsync(db, "Green Leaf Catering", BusinessCategory.CATERING, VendorStatus.APPROVED);
        var service = CreateService(db);

        var result = await service.ListAsync(new VendorMarketplaceQuery { Category = "CATERING" });

        Assert.Single(result.Items);
        Assert.Equal("CATERING", result.Items[0].Category);
    }

    [Fact]
    public async Task ListAsync_SortsByBusinessName()
    {
        using var db = CreateDb();
        await SeedVendorAsync(db, "Zebra Venue", BusinessCategory.VENUE, VendorStatus.APPROVED);
        await SeedVendorAsync(db, "Alpha Music", BusinessCategory.MUSIC, VendorStatus.APPROVED);
        var service = CreateService(db);

        var result = await service.ListAsync(new VendorMarketplaceQuery
        {
            SortBy = "businessName",
            SortOrder = "asc"
        });

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Alpha Music", result.Items[0].BusinessName);
        Assert.Equal("Zebra Venue", result.Items[1].BusinessName);
    }

    [Fact]
    public async Task ListAsync_PaginatesResults()
    {
        using var db = CreateDb();
        await SeedVendorAsync(db, "A Vendor", BusinessCategory.VENUE, VendorStatus.APPROVED);
        await SeedVendorAsync(db, "B Vendor", BusinessCategory.VENUE, VendorStatus.APPROVED);
        await SeedVendorAsync(db, "C Vendor", BusinessCategory.VENUE, VendorStatus.APPROVED);
        var service = CreateService(db);

        var result = await service.ListAsync(new VendorMarketplaceQuery
        {
            Page = 2,
            PageSize = 2,
            SortBy = "businessName",
            SortOrder = "asc"
        });

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Single(result.Items);
        Assert.Equal("C Vendor", result.Items[0].BusinessName);
    }

    [Fact]
    public async Task ListAsync_IncludesStartingPrice_WithoutPrivateFields()
    {
        using var db = CreateDb();
        var vendor = await SeedVendorAsync(db, "KG Photography", BusinessCategory.PHOTOGRAPHY, VendorStatus.APPROVED, "Wedding photography");
        db.VendorOfferings.AddRange(
            new VendorOffering
            {
                Id = Guid.NewGuid(),
                VendorId = vendor.Id,
                ServiceName = "Birthday",
                Price = 40000m,
                PricingType = PricingType.FIXED,
                CreatedAt = DateTime.UtcNow
            },
            new VendorOffering
            {
                Id = Guid.NewGuid(),
                VendorId = vendor.Id,
                ServiceName = "Wedding",
                Price = 75000m,
                PricingType = PricingType.FIXED,
                CreatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.ListAsync(new VendorMarketplaceQuery());
        var json = JsonSerializer.Serialize(result.Items[0]);

        Assert.Equal(40000m, result.Items[0].StartingPrice);
        Assert.Equal("FIXED", result.Items[0].StartingPricingType);
        Assert.DoesNotContain("contactEmail", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contactPhone", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("userId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@private.test", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDetail_WithServicesGalleryAndAvailability()
    {
        using var db = CreateDb();
        var vendor = await SeedVendorAsync(db, "KG Photography", BusinessCategory.PHOTOGRAPHY, VendorStatus.APPROVED, "Full service");
        db.VendorOfferings.Add(new VendorOffering
        {
            Id = Guid.NewGuid(),
            VendorId = vendor.Id,
            ServiceName = "Wedding Photography",
            Description = "Full-day wedding photography",
            Price = 75000m,
            PricingType = PricingType.FIXED,
            CreatedAt = DateTime.UtcNow
        });
        db.VendorGalleryImages.Add(new VendorGalleryImage
        {
            Id = Guid.NewGuid(),
            VendorId = vendor.Id,
            ImageUrl = "/uploads/vendors/gallery1.jpg",
            CreatedAt = DateTime.UtcNow
        });
        db.VendorAvailabilities.Add(new VendorAvailability
        {
            Id = Guid.NewGuid(),
            VendorId = vendor.Id,
            StartDateTime = DateTime.UtcNow.AddDays(5),
            EndDateTime = DateTime.UtcNow.AddDays(5).AddHours(8),
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var detail = await service.GetByIdAsync(vendor.Id);
        var json = JsonSerializer.Serialize(detail);

        Assert.Equal("KG Photography", detail.BusinessName);
        Assert.Equal("https://example.test", detail.WebsiteUrl);
        Assert.Single(detail.Services);
        Assert.Equal(75000m, detail.Services[0].Price);
        Assert.Equal("FIXED", detail.Services[0].PricingType);
        Assert.Single(detail.Images);
        Assert.Single(detail.Availability);
        Assert.DoesNotContain("contactEmail", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contactPhone", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("userId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@private.test", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetByIdAsync_Throws_WhenVendorIsPending()
    {
        using var db = CreateDb();
        var vendor = await SeedVendorAsync(db, "Pending Studio", BusinessCategory.PHOTOGRAPHY, VendorStatus.PENDING);
        var service = CreateService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetByIdAsync(vendor.Id));
    }

    [Fact]
    public async Task GetByIdAsync_Throws_WhenVendorIsSuspended()
    {
        using var db = CreateDb();
        var vendor = await SeedVendorAsync(db, "Suspended Studio", BusinessCategory.PHOTOGRAPHY, VendorStatus.SUSPEND);
        var service = CreateService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetByIdAsync(vendor.Id));
    }
}
