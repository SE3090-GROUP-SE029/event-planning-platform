using Application.Common.Interfaces;
using Application.Dtos.Vendors;
using Application.Services.Vendors;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.UnitTests;

public class VendorRecommendationCandidateBuilderTests
{
    [Theory]
    [InlineData("Catering", BusinessCategory.CATERING)]
    [InlineData("Photography provider", BusinessCategory.PHOTOGRAPHY)]
    [InlineData("Venue", BusinessCategory.VENUE)]
    [InlineData("DJ / Entertainment", BusinessCategory.MUSIC)]
    [InlineData("Floral decor", BusinessCategory.FLORIST)]
    [InlineData("Transportation", BusinessCategory.TRANSPORTATION)]
    public void TryMapCategory_MapsCommonPlanLabels(string raw, BusinessCategory expected)
    {
        Assert.True(VendorRecommendationCandidateBuilder.TryMapCategory(raw, out var mapped));
        Assert.Equal(expected, mapped);
    }

    [Fact]
    public void Build_ExcludesNonApprovedVendors()
    {
        var approved = MakeVendor("Approved", VendorStatus.APPROVED, BusinessCategory.CATERING);
        var pending = MakeVendor("Pending", VendorStatus.PENDING, BusinessCategory.CATERING);

        var result = VendorRecommendationCandidateBuilder.Build(
            [approved, pending],
            new Dictionary<Guid, IReadOnlyList<VendorOffering>>(),
            new Dictionary<Guid, IReadOnlyList<VendorAvailability>>(),
            new Dictionary<Guid, (decimal, int)>(),
            [BusinessCategory.CATERING],
            new Dictionary<string, decimal> { ["Catering"] = 5000 },
            10000,
            100,
            DateTime.UtcNow.Date.AddDays(30));

        Assert.Single(result);
        Assert.Equal(approved.Id, result[0].Vendor.Id);
    }

    [Fact]
    public void Build_FiltersByCategory()
    {
        var catering = MakeVendor("Caterer", VendorStatus.APPROVED, BusinessCategory.CATERING);
        var photo = MakeVendor("Photo", VendorStatus.APPROVED, BusinessCategory.PHOTOGRAPHY);

        var result = VendorRecommendationCandidateBuilder.Build(
            [catering, photo],
            new Dictionary<Guid, IReadOnlyList<VendorOffering>>(),
            new Dictionary<Guid, IReadOnlyList<VendorAvailability>>(),
            new Dictionary<Guid, (decimal, int)>(),
            [BusinessCategory.CATERING],
            new Dictionary<string, decimal>(),
            10000,
            50,
            DateTime.UtcNow.Date.AddDays(40));

        Assert.Single(result);
        Assert.Equal(BusinessCategory.CATERING, result[0].Vendor.Category);
    }

    [Fact]
    public void Build_ExcludesVendorsOverBudget()
    {
        var vendor = MakeVendor("Pricey", VendorStatus.APPROVED, BusinessCategory.CATERING);
        var offering = new VendorOffering
        {
            Id = Guid.NewGuid(),
            VendorId = vendor.Id,
            ServiceName = "Gold Buffet",
            Price = 200,
            PricingType = PricingType.PER_PERSON
        };

        var result = VendorRecommendationCandidateBuilder.Build(
            [vendor],
            new Dictionary<Guid, IReadOnlyList<VendorOffering>>
            {
                [vendor.Id] = [offering]
            },
            new Dictionary<Guid, IReadOnlyList<VendorAvailability>>(),
            new Dictionary<Guid, (decimal, int)>(),
            [BusinessCategory.CATERING],
            new Dictionary<string, decimal> { ["Catering"] = 5000 },
            10000,
            guestCount: 100, // effective 20000 > 5000
            DateTime.UtcNow.Date.AddDays(40));

        Assert.Empty(result);
    }

    [Fact]
    public void Build_ExcludesBlockedAvailability()
    {
        var vendor = MakeVendor("Busy", VendorStatus.APPROVED, BusinessCategory.VENUE);
        var eventDate = DateTime.UtcNow.Date.AddDays(45);
        var blocked = new VendorAvailability
        {
            Id = Guid.NewGuid(),
            VendorId = vendor.Id,
            StartDateTime = eventDate.AddHours(-1),
            EndDateTime = eventDate.AddDays(1),
            IsAvailable = false
        };

        var result = VendorRecommendationCandidateBuilder.Build(
            [vendor],
            new Dictionary<Guid, IReadOnlyList<VendorOffering>>(),
            new Dictionary<Guid, IReadOnlyList<VendorAvailability>>
            {
                [vendor.Id] = [blocked]
            },
            new Dictionary<Guid, (decimal, int)>(),
            [BusinessCategory.VENUE],
            new Dictionary<string, decimal>(),
            10000,
            80,
            eventDate);

        Assert.Empty(result);
    }

    [Fact]
    public void Build_MapsRatingsAndAvailabilityMatch()
    {
        var vendor = MakeVendor("Rated", VendorStatus.APPROVED, BusinessCategory.PHOTOGRAPHY);
        var eventDate = DateTime.UtcNow.Date.AddDays(60);
        var available = new VendorAvailability
        {
            Id = Guid.NewGuid(),
            VendorId = vendor.Id,
            StartDateTime = eventDate,
            EndDateTime = eventDate.AddDays(1),
            IsAvailable = true
        };

        var result = VendorRecommendationCandidateBuilder.Build(
            [vendor],
            new Dictionary<Guid, IReadOnlyList<VendorOffering>>(),
            new Dictionary<Guid, IReadOnlyList<VendorAvailability>>
            {
                [vendor.Id] = [available]
            },
            new Dictionary<Guid, (decimal, int)>
            {
                [vendor.Id] = (4.5m, 8)
            },
            [BusinessCategory.PHOTOGRAPHY],
            new Dictionary<string, decimal>(),
            10000,
            50,
            eventDate);

        Assert.Single(result);
        Assert.True(result[0].AvailabilityMatch);
        Assert.Equal(4.5m, result[0].AverageRating);
        Assert.Equal(8, result[0].ReviewCount);
    }

    private static Vendor MakeVendor(string name, VendorStatus status, BusinessCategory category) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BusinessName = name,
            Category = category,
            ContactEmail = $"{name.Replace(" ", "").ToLowerInvariant()}@test.local",
            ContactPhone = "0770000000",
            Address = "1 Test Street",
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
}

public class VendorRecommendationServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class FakeAiClient : IVendorAnalysisAiClient
    {
        public VendorAnalysisRecommendResponse? Response { get; set; }
        public Exception? Exception { get; set; }
        public VendorAnalysisRecommendRequest? LastRequest { get; private set; }
        public int Calls { get; private set; }

        public Task<VendorAnalysisRecommendResponse> RecommendAsync(
            VendorAnalysisRecommendRequest request,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            LastRequest = request;
            if (Exception is not null)
                throw Exception;
            return Task.FromResult(Response ?? new VendorAnalysisRecommendResponse());
        }
    }

    private static async Task<(AppDbContext Db, Event Evt, EventPlanDraft Plan, Vendor Vendor, VendorOffering Offering)>
        SeedApprovedPlanAsync(AppDbContext db)
    {
        var ownerId = Guid.NewGuid();
        var evt = new Event
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            EventName = "Wedding",
            EventType = EventType.WEDDING,
            GuestCount = 100,
            Budget = 20000,
            PreferredVenue = "Garden",
            PreferredDate = DateTime.UtcNow.AddDays(90),
            EventDuration = TimeSpan.FromHours(6),
            Status = EventStatus.DRAFT,
            CreatedAt = DateTime.UtcNow
        };
        db.Events.Add(evt);

        var plan = new EventPlanDraft
        {
            Id = Guid.NewGuid(),
            EventId = evt.Id,
            Version = 1,
            Status = PlanStatus.Approved,
            ServiceCategories = ["Catering"],
            TargetVendorTypes = ["Catering provider"],
            BudgetAllocation = new Dictionary<string, decimal> { ["Catering"] = 8000 },
            ProposedTimeline = new Dictionary<string, string> { ["Planning"] = "Soon" },
            Rationale = "Test",
            PlanCompletenessScore = 80,
            ValidationSummary = "ok",
            EventSnapshot = EventSnapshot.FromPersistedData(
                evt.EventName,
                evt.EventType,
                evt.PreferredDate,
                evt.PreferredVenue,
                evt.GuestCount,
                evt.Budget,
                [],
                evt.CreatedAt,
                evt.CreatedAt),
            GeneratedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedById = ownerId
        };
        db.EventPlanDrafts.Add(plan);

        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BusinessName = "Taste Co",
            Category = BusinessCategory.CATERING,
            ContactEmail = "taste@test.local",
            ContactPhone = "077",
            Address = "Addr",
            Status = VendorStatus.APPROVED,
            CreatedAt = DateTime.UtcNow
        };
        db.Vendors.Add(vendor);

        var offering = new VendorOffering
        {
            Id = Guid.NewGuid(),
            VendorId = vendor.Id,
            ServiceName = "Buffet",
            Price = 40,
            PricingType = PricingType.PER_PERSON,
            CreatedAt = DateTime.UtcNow
        };
        db.VendorOfferings.Add(offering);
        await db.SaveChangesAsync();
        return (db, evt, plan, vendor, offering);
    }

    private static VendorRecommendationService CreateService(AppDbContext db, FakeAiClient ai) =>
        new(
            new EventRepository(db),
            new EventPlanDraftRepository(db),
            new VendorRecommendationRepository(db),
            new VendorRatingRepository(db),
            ai,
            NullLogger<VendorRecommendationService>.Instance);

    [Fact]
    public async Task GenerateAsync_Throws_WhenUserDoesNotOwnEvent()
    {
        using var db = CreateDb();
        var seeded = await SeedApprovedPlanAsync(db);
        var service = CreateService(db, new FakeAiClient());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GenerateAsync(seeded.Evt.Id, Guid.NewGuid(), null));
    }

    [Fact]
    public async Task GenerateAsync_RequiresApprovedPlan()
    {
        using var db = CreateDb();
        var seeded = await SeedApprovedPlanAsync(db);
        seeded.Plan.Status = PlanStatus.PendingPlannerReview;
        await db.SaveChangesAsync();
        var service = CreateService(db, new FakeAiClient());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateAsync(seeded.Evt.Id, seeded.Evt.OwnerId, null));
        Assert.Contains("Approved", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAsync_ReusesActiveRun_ForDuplicateRequests()
    {
        using var db = CreateDb();
        var seeded = await SeedApprovedPlanAsync(db);
        var ai = new FakeAiClient();
        var service = CreateService(db, ai);

        var first = await service.GenerateAsync(seeded.Evt.Id, seeded.Evt.OwnerId, null);
        var duplicate = await service.GenerateAsync(seeded.Evt.Id, seeded.Evt.OwnerId, null);

        Assert.Equal(first.Id, duplicate.Id);
        Assert.Equal(VendorRecommendationRun.PendingStatus, duplicate.Status);
        Assert.Equal(1, await db.VendorRecommendationRuns.CountAsync());
        Assert.Equal(0, ai.Calls);
    }

    [Fact]
    public async Task ProcessNextAsync_DropsUnknownAiVendorIds_AndPersistsValidOnes()
    {
        using var db = CreateDb();
        var seeded = await SeedApprovedPlanAsync(db);
        var ai = new FakeAiClient
        {
            Response = new VendorAnalysisRecommendResponse
            {
                Recommendations =
                [
                    new VendorAnalysisRecommendItemDto
                    {
                        VendorId = Guid.NewGuid(),
                        Score = 99,
                        Reasons = ["Invented"]
                    },
                    new VendorAnalysisRecommendItemDto
                    {
                        VendorId = seeded.Vendor.Id,
                        VendorServiceId = seeded.Offering.Id,
                        Score = 88,
                        Reasons = ["Fits catering budget"]
                    }
                ]
            }
        };
        var service = CreateService(db, ai);

        var queued = await service.GenerateAsync(seeded.Evt.Id, seeded.Evt.OwnerId, null);
        Assert.Equal(VendorRecommendationRun.PendingStatus, queued.Status);
        Assert.Empty(queued.Items);
        Assert.Equal(0, ai.Calls);

        Assert.True(await service.ProcessNextAsync());
        var result = await service.GetLatestAsync(seeded.Evt.Id, seeded.Evt.OwnerId);

        Assert.NotNull(result);
        Assert.True(
            result!.Status == VendorRecommendationRun.CompletedStatus,
            $"Expected Completed, got {result.Status}: {result.FailureMessage}");
        Assert.Single(result.Items);
        Assert.Equal(seeded.Vendor.Id, result.Items[0].VendorId);
        Assert.Equal(88, result.Items[0].Score);
        Assert.Contains("Fits catering budget", result.Items[0].Reason);
        Assert.Equal(1, await db.VendorRecommendationRuns.CountAsync());
        Assert.Equal(1, await db.VendorRecommendationItems.CountAsync());
    }

    [Fact]
    public async Task GenerateAsync_ReusesCompletedRun_AndRecordsRefreshFailure()
    {
        using var db = CreateDb();
        var seeded = await SeedApprovedPlanAsync(db);
        var ai = new FakeAiClient
        {
            Response = new VendorAnalysisRecommendResponse
            {
                Recommendations =
                [
                    new VendorAnalysisRecommendItemDto
                    {
                        VendorId = seeded.Vendor.Id,
                        VendorServiceId = seeded.Offering.Id,
                        Score = 80,
                        Reasons = ["First run"]
                    }
                ]
            }
        };
        var service = CreateService(db, ai);
        var queued = await service.GenerateAsync(seeded.Evt.Id, seeded.Evt.OwnerId, null);
        await service.ProcessNextAsync();

        ai.Exception = new HttpRequestException("down", null, System.Net.HttpStatusCode.ServiceUnavailable);
        var cached = await service.GenerateAsync(seeded.Evt.Id, seeded.Evt.OwnerId, null);
        Assert.Equal(queued.Id, cached.Id);
        Assert.Equal(VendorRecommendationRun.CompletedStatus, cached.Status);
        Assert.Equal(1, ai.Calls);

        var refresh = await service.GenerateAsync(
            seeded.Evt.Id,
            seeded.Evt.OwnerId,
            new GenerateVendorRecommendationsRequest { ForceRefresh = true });
        Assert.Equal(VendorRecommendationRun.PendingStatus, refresh.Status);
        Assert.True(await service.ProcessNextAsync());

        var failed = await service.GetRunAsync(
            seeded.Evt.Id,
            refresh.Id,
            seeded.Evt.OwnerId);
        Assert.NotNull(failed);
        Assert.Equal(VendorRecommendationRun.FailedStatus, failed!.Status);
        Assert.Contains("down", failed.FailureMessage);
        Assert.Equal(2, ai.Calls);
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsPersistedRun()
    {
        using var db = CreateDb();
        var seeded = await SeedApprovedPlanAsync(db);
        var ai = new FakeAiClient
        {
            Response = new VendorAnalysisRecommendResponse
            {
                Recommendations =
                [
                    new VendorAnalysisRecommendItemDto
                    {
                        VendorId = seeded.Vendor.Id,
                        Score = 70,
                        Reasons = ["Good fit"]
                    }
                ]
            }
        };
        var service = CreateService(db, ai);
        await service.GenerateAsync(seeded.Evt.Id, seeded.Evt.OwnerId, null);
        await service.ProcessNextAsync();

        var latest = await service.GetLatestAsync(seeded.Evt.Id, seeded.Evt.OwnerId);
        Assert.NotNull(latest);
        Assert.True(
            latest!.Status == VendorRecommendationRun.CompletedStatus,
            $"Expected Completed, got {latest.Status}: {latest.FailureMessage}");
        Assert.Single(latest!.Items);
    }
}
