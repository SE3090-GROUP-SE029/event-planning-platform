using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Services.Planning;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.UnitTests;

public sealed class PlanDecisionServiceTests
{
    [Fact]
    public async Task ApprovePlanAsync_AllowsEventOwner()
    {
        var ownerId = Guid.NewGuid();
        var plan = CreatePlan(ownerId);
        var repository = new TestPlanRepository(plan);
        var service = CreateService(repository, ownerId, false);

        var result = await service.ApprovePlanAsync(plan.Id, "Looks good.");

        Assert.Same(plan, result);
        Assert.Equal(PlanStatus.Approved, plan.Status);
        Assert.Equal(ownerId, plan.ApprovedById);
        Assert.Equal(1, repository.SaveCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ApprovePlanAsync_RejectsNonOwnerAndAdmin(bool isAdmin)
    {
        var plan = CreatePlan(Guid.NewGuid());
        var repository = new TestPlanRepository(plan);
        var service = CreateService(repository, Guid.NewGuid(), isAdmin);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ApprovePlanAsync(plan.Id));

        Assert.Equal(PlanStatus.PendingPlannerReview, plan.Status);
        Assert.Equal(0, repository.SaveCount);
    }

    private static EventPlanDraft CreatePlan(Guid ownerId)
    {
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId
        };
        return new EventPlanDraft
        {
            Id = Guid.NewGuid(),
            EventId = eventEntity.Id,
            Event = eventEntity,
            Status = PlanStatus.PendingPlannerReview
        };
    }

    private static PlanDecisionService CreateService(
        TestPlanRepository repository,
        Guid? userId,
        bool isAdmin) =>
        new(
            repository,
            new TestCurrentUserService(userId, isAdmin),
            NullLogger<PlanDecisionService>.Instance);

    private sealed class TestCurrentUserService(Guid? userId, bool isAdmin) : ICurrentUserService
    {
        public Guid? UserId { get; } = userId;
        public bool IsAdmin { get; } = isAdmin;
    }

    private sealed class TestPlanRepository(EventPlanDraft plan) : IEventPlanDraftRepository
    {
        public int SaveCount { get; private set; }

        public Task<EventPlanDraft?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<EventPlanDraft?>(id == plan.Id ? plan : null);

        public Task<IReadOnlyList<EventPlanDraft>> ListAsync(
            Guid? eventId,
            PlanStatus? status,
            int? version,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<int> GetNextVersionAsync(
            Guid eventId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddAsync(EventPlanDraft newPlan, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
