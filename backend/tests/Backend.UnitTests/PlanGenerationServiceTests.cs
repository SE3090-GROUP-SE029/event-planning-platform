using Application.Common.Interfaces;
using Application.Dtos.Events;
using Application.Dtos.Plans;
using Application.Services.Planning;
using Application.Services.Validation;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Services.Planning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.UnitTests;

public sealed class PlanGenerationServiceTests
{
    [Fact]
    public async Task GeneratePlanAsync_AllowsEventOwner()
    {
        var eventOwner = Guid.NewGuid();
        var eventEntity = CreateEvent(eventOwner);
        using var db = CreateDb();
        var aiClient = new TestAgenticAiClient();
        var planRepository = new TestEventPlanDraftRepository();
        var service = CreateService(db, eventEntity, eventOwner, false, aiClient, planRepository);

        var plan = await service.GeneratePlanAsync(eventEntity.Id);

        Assert.Equal(eventOwner, plan.CreatedById);
        Assert.Equal(PlanStatus.PendingPlannerReview, plan.Status);
        Assert.Same(plan, planRepository.AddedPlan);
        Assert.Equal(1, aiClient.CallCount);
    }

    [Fact]
    public async Task GeneratePlanAsync_RejectsAdminForAnotherUsersEvent()
    {
        var eventEntity = CreateEvent(Guid.NewGuid());
        var adminId = Guid.NewGuid();
        using var db = CreateDb();
        var aiClient = new TestAgenticAiClient();
        var planRepository = new TestEventPlanDraftRepository();
        var service = CreateService(db, eventEntity, adminId, true, aiClient, planRepository);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GeneratePlanAsync(eventEntity.Id));

        Assert.Null(planRepository.AddedPlan);
        Assert.Equal(0, aiClient.CallCount);
    }

    [Fact]
    public async Task GeneratePlanAsync_RejectsUserWhoDoesNotOwnEvent()
    {
        var eventEntity = CreateEvent(Guid.NewGuid());
        var userId = Guid.NewGuid();
        using var db = CreateDb();
        var aiClient = new TestAgenticAiClient();
        var planRepository = new TestEventPlanDraftRepository();
        var service = CreateService(db, eventEntity, userId, false, aiClient, planRepository);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GeneratePlanAsync(eventEntity.Id));

        Assert.Null(planRepository.AddedPlan);
        Assert.Equal(0, aiClient.CallCount);
    }

    [Fact]
    public async Task GeneratePlanAsync_RejectsUserWithoutIdentity()
    {
        var eventEntity = CreateEvent(Guid.NewGuid());
        using var db = CreateDb();
        var aiClient = new TestAgenticAiClient();
        var planRepository = new TestEventPlanDraftRepository();
        var service = CreateService(db, eventEntity, null, false, aiClient, planRepository);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GeneratePlanAsync(eventEntity.Id));

        Assert.Null(planRepository.AddedPlan);
        Assert.Equal(0, aiClient.CallCount);
    }

    [Fact]
    public async Task GeneratePlanAsync_OnlyOwnerCanRegenerateRejectedPlan()
    {
        var ownerId = Guid.NewGuid();
        var eventEntity = CreateEvent(ownerId);
        var rejectedPlan = new EventPlanDraft
        {
            Id = Guid.NewGuid(),
            EventId = eventEntity.Id,
            Event = eventEntity,
            Version = 1,
            Status = PlanStatus.Rejected,
            EventSnapshot = EventSnapshot.FromEvent(eventEntity)
        };
        using var db = CreateDb();
        var aiClient = new TestAgenticAiClient();
        var planRepository = new TestEventPlanDraftRepository();
        planRepository.AddExisting(rejectedPlan);
        var service = CreateService(db, eventEntity, ownerId, false, aiClient, planRepository);

        var plan = await service.GeneratePlanAsync(eventEntity.Id, regenerate: true);

        Assert.Equal(2, plan.Version);
        Assert.Equal(PlanStatus.Superseded, rejectedPlan.Status);
        Assert.Equal(PlanStatus.PendingPlannerReview, plan.Status);
        Assert.Equal(1, aiClient.CallCount);
    }

    [Fact]
    public async Task GeneratePlanAsync_RejectsAdminRegeneration()
    {
        var eventEntity = CreateEvent(Guid.NewGuid());
        var rejectedPlan = new EventPlanDraft
        {
            Id = Guid.NewGuid(),
            EventId = eventEntity.Id,
            Event = eventEntity,
            Version = 1,
            Status = PlanStatus.Rejected,
            EventSnapshot = EventSnapshot.FromEvent(eventEntity)
        };
        using var db = CreateDb();
        var aiClient = new TestAgenticAiClient();
        var planRepository = new TestEventPlanDraftRepository();
        planRepository.AddExisting(rejectedPlan);
        var service = CreateService(db, eventEntity, Guid.NewGuid(), true, aiClient, planRepository);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GeneratePlanAsync(eventEntity.Id, regenerate: true));

        Assert.Equal(PlanStatus.Rejected, rejectedPlan.Status);
        Assert.Equal(0, aiClient.CallCount);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings =>
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static Event CreateEvent(Guid ownerId) =>
        new()
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            EventType = EventType.WEDDING,
            GuestCount = 50,
            Budget = 1000m,
            PreferredVenue = "Community Hall",
            PreferredDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };

    private static PlanGenerationService CreateService(
        AppDbContext db,
        Event eventEntity,
        Guid userId,
        bool isAdmin,
        TestAgenticAiClient aiClient,
        TestEventPlanDraftRepository planRepository) =>
        new(
            new TestEventRepository(eventEntity),
            planRepository,
            new TestCurrentUserService(userId, isAdmin),
            aiClient,
            new CoordinatorPlanValidationService(
                NullLogger<CoordinatorPlanValidationService>.Instance),
            db,
            NullLogger<PlanGenerationService>.Instance);

    private sealed class TestCurrentUserService(Guid? userId, bool isAdmin) : ICurrentUserService
    {
        public Guid? UserId { get; } = userId;
        public bool IsAdmin { get; } = isAdmin;
    }

    private sealed class TestEventRepository(Event eventEntity) : IEventRepository
    {
        public Task<Event?> GetByIdAsync(Guid id) =>
            Task.FromResult<Event?>(id == eventEntity.Id ? eventEntity : null);

        public Task<(IReadOnlyList<Event> Items, int TotalCount)> ListAsync(
            Guid? ownerId,
            EventQuery query) =>
            throw new NotImplementedException();

        public Task<(IReadOnlyList<Event> Items, int TotalCount)> ListAdminAsync(
            AdminEventQuery query) =>
            throw new NotImplementedException();

        public Task AddAsync(Event eventEntity) => throw new NotImplementedException();
        public Task SaveChangesAsync() => throw new NotImplementedException();
        public void Remove(Event eventEntity) => throw new NotImplementedException();
    }

    private sealed class TestEventPlanDraftRepository : IEventPlanDraftRepository
    {
        private readonly List<EventPlanDraft> _plans = [];
        public EventPlanDraft? AddedPlan { get; private set; }

        public void AddExisting(EventPlanDraft plan) => _plans.Add(plan);

        public Task<EventPlanDraft?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_plans.FirstOrDefault(plan => plan.Id == id));

        public Task<IReadOnlyList<EventPlanDraft>> ListAsync(
            Guid? eventId,
            PlanStatus? status,
            int? version,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EventPlanDraft>>(_plans
                .Where(plan => !eventId.HasValue || plan.EventId == eventId.Value)
                .Where(plan => !status.HasValue || plan.Status == status.Value)
                .Where(plan => !version.HasValue || plan.Version == version.Value)
                .OrderByDescending(plan => plan.Version)
                .ToList());

        public Task<int> GetNextVersionAsync(
            Guid eventId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_plans
                .Where(plan => plan.EventId == eventId)
                .Select(plan => plan.Version)
                .DefaultIfEmpty()
                .Max() + 1);

        public Task AddAsync(
            EventPlanDraft plan,
            CancellationToken cancellationToken = default)
        {
            AddedPlan = plan;
            _plans.Add(plan);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class TestAgenticAiClient : IAgenticAiClient
    {
        public int CallCount { get; private set; }

        public Task<CoordinatorPlanResponse> GeneratePlanAsync(
            Event eventEntity,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new CoordinatorPlanResponse
            {
                ServiceCategories = ["Venue", "Catering"],
                BudgetAllocation = new Dictionary<string, decimal>
                {
                    ["Venue"] = eventEntity.Budget / 2,
                    ["Catering"] = eventEntity.Budget / 2
                },
                TargetVendorTypes = ["Venue provider"],
                ProposedTimeline = new Dictionary<string, string>
                {
                    ["Planning"] = "Begin planning",
                    ["Booking"] = "Book core services",
                    ["Confirmation"] = "Confirm final details"
                },
                Rationale = new string('A', 120),
                PlanCompletenessScore = 80
            });
        }
    }
}
