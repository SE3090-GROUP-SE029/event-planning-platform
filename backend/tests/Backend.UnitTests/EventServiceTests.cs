using Application.Common.Interfaces;
using Application.Dtos.Events;
using Application.Services.Events;
using Application.Validators.Events;
using Domain.Entities;
using Domain.Enums;

namespace Backend.UnitTests;

public class EventServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesDraftEventForOwner()
    {
        var repository = new TestEventRepository();
        var service = new EventService(repository, new CreateEventRequestValidator());
        var ownerId = Guid.NewGuid();

        var result = await service.CreateAsync(ownerId, ValidRequest());

        Assert.NotNull(repository.AddedEvent);
        Assert.Equal(ownerId, repository.AddedEvent!.OwnerId);
        Assert.Equal(EventStatus.DRAFT, repository.AddedEvent.Status);
        Assert.Equal(repository.AddedEvent.CreatedAt, repository.AddedEvent.UpdatedAt);
        Assert.Equal(repository.AddedEvent.Id, result.Id);
        Assert.Equal(ownerId, result.OwnerId);
        Assert.Equal(EventStatus.DRAFT, result.Status);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task CreateAsync_RejectsInvalidRequest()
    {
        var repository = new TestEventRepository();
        var service = new EventService(repository, new CreateEventRequestValidator());
        var request = ValidRequest();
        request.GuestCount = 0;

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            service.CreateAsync(Guid.NewGuid(), request));

        Assert.Null(repository.AddedEvent);
        Assert.False(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEventForOwner()
    {
        var ownerId = Guid.NewGuid();
        var eventEntity = CreateEvent(ownerId);
        var service = new EventService(new TestEventRepository(eventEntity), new CreateEventRequestValidator());

        var result = await service.GetByIdAsync(eventEntity.Id, ownerId, isAdmin: false);

        Assert.Equal(eventEntity.Id, result.Id);
        Assert.Equal(ownerId, result.OwnerId);
    }

    [Fact]
    public async Task GetByIdAsync_RejectsNonOwner()
    {
        var eventEntity = CreateEvent(Guid.NewGuid());
        var service = new EventService(new TestEventRepository(eventEntity), new CreateEventRequestValidator());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetByIdAsync(eventEntity.Id, Guid.NewGuid(), isAdmin: false));
    }

    [Fact]
    public async Task GetByIdAsync_AllowsAdminToAccessAnyEvent()
    {
        var eventEntity = CreateEvent(Guid.NewGuid());
        var service = new EventService(new TestEventRepository(eventEntity), new CreateEventRequestValidator());

        var result = await service.GetByIdAsync(eventEntity.Id, Guid.NewGuid(), isAdmin: true);

        Assert.Equal(eventEntity.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsWhenEventDoesNotExist()
    {
        var service = new EventService(new TestEventRepository(), new CreateEventRequestValidator());

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid(), isAdmin: false));
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlyCurrentUsersEvents()
    {
        var ownerId = Guid.NewGuid();
        var repository = new TestEventRepository(CreateEvent(ownerId), CreateEvent(Guid.NewGuid()));
        var service = new EventService(repository, new CreateEventRequestValidator());

        var result = await service.ListAsync(ownerId, isAdmin: false, new EventQuery());

        Assert.Single(result.Items);
        Assert.Equal(ownerId, result.Items[0].OwnerId);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task ListAsync_AllowsAdminToSeeAllEvents()
    {
        var repository = new TestEventRepository(CreateEvent(Guid.NewGuid()), CreateEvent(Guid.NewGuid()));
        var service = new EventService(repository, new CreateEventRequestValidator());

        var result = await service.ListAsync(Guid.NewGuid(), isAdmin: true, new EventQuery());

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task ListAsync_AppliesFiltersAndPagination()
    {
        var ownerId = Guid.NewGuid();
        var draft = CreateEvent(ownerId);
        draft.Status = EventStatus.DRAFT;
        var confirmed = CreateEvent(ownerId);
        confirmed.Status = EventStatus.CONFIRMED;
        var repository = new TestEventRepository(draft, confirmed);
        var service = new EventService(repository, new CreateEventRequestValidator());

        var result = await service.ListAsync(ownerId, false, new EventQuery
        {
            Status = EventStatus.CONFIRMED,
            Page = 1,
            PageSize = 1
        });

        Assert.Single(result.Items);
        Assert.Equal(EventStatus.CONFIRMED, result.Items[0].Status);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOwnerEventAndTimestamp()
    {
        var eventEntity = CreateEvent(Guid.NewGuid());
        var originalUpdatedAt = eventEntity.UpdatedAt;
        var service = new EventService(new TestEventRepository(eventEntity), new CreateEventRequestValidator());

        var result = await service.UpdateAsync(eventEntity.Id, eventEntity.OwnerId, new UpdateEventRequest
        {
            EventType = EventType.WEDDING,
            GuestCount = 80,
            Budget = 4000,
            PreferredVenue = "Updated Hall",
            PreferredDate = DateTime.UtcNow.AddDays(20),
            Requirements = "Parking",
            Status = EventStatus.CONFIRMED
        });

        Assert.Equal(EventType.WEDDING, result.EventType);
        Assert.Equal(EventStatus.CONFIRMED, result.Status);
        Assert.NotEqual(originalUpdatedAt, result.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_RejectsNonOwner()
    {
        var eventEntity = CreateEvent(Guid.NewGuid());
        var service = new EventService(new TestEventRepository(eventEntity), new CreateEventRequestValidator());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateAsync(eventEntity.Id, Guid.NewGuid(), new UpdateEventRequest()));
    }

    [Fact]
    public async Task DeleteAsync_DeletesOwnerEvent()
    {
        var eventEntity = CreateEvent(Guid.NewGuid());
        var repository = new TestEventRepository(eventEntity);
        var service = new EventService(repository, new CreateEventRequestValidator());

        await service.DeleteAsync(eventEntity.Id, eventEntity.OwnerId);

        Assert.Null(await repository.GetByIdAsync(eventEntity.Id));
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task DeleteAsync_RejectsNonOwner()
    {
        var eventEntity = CreateEvent(Guid.NewGuid());
        var service = new EventService(new TestEventRepository(eventEntity), new CreateEventRequestValidator());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.DeleteAsync(eventEntity.Id, Guid.NewGuid()));
    }

    private static CreateEventRequest ValidRequest() => new()
    {
        EventType = EventType.CORPORATE,
        GuestCount = 50,
        Budget = 2500,
        PreferredVenue = "Conference Hall",
        PreferredDate = DateTime.UtcNow.AddDays(14),
        EventDuration = TimeSpan.FromHours(3),
        Requirements = "Projector"
    };

    private static Event CreateEvent(Guid ownerId) => new()
    {
        Id = Guid.NewGuid(),
        OwnerId = ownerId,
        EventType = EventType.CORPORATE,
        GuestCount = 50,
        Budget = 2500,
        PreferredVenue = "Conference Hall",
        PreferredDate = DateTime.UtcNow.AddDays(14),
        EventDuration = TimeSpan.FromHours(3),
        Status = EventStatus.DRAFT,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private sealed class TestEventRepository(params Event[] events) : IEventRepository
    {
        private readonly List<Event> _events = events.ToList();

        public Event? AddedEvent { get; private set; }
        public bool SaveChangesCalled { get; private set; }

        public Task<Event?> GetByIdAsync(Guid id) =>
            Task.FromResult(_events.SingleOrDefault(eventEntity => eventEntity.Id == id));

        public Task<(IReadOnlyList<Event> Items, int TotalCount)> ListAsync(Guid? ownerId, EventQuery query)
        {
            var filtered = _events
                .Where(eventEntity => !ownerId.HasValue || eventEntity.OwnerId == ownerId.Value)
                .Where(eventEntity => !query.Status.HasValue || eventEntity.Status == query.Status.Value)
                .Where(eventEntity => !query.EventType.HasValue || eventEntity.EventType == query.EventType.Value)
                .ToList();

            return Task.FromResult(((IReadOnlyList<Event>)filtered, filtered.Count));
        }

        public Task AddAsync(Event eventEntity)
        {
            AddedEvent = eventEntity;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }

        public void Remove(Event eventEntity) => _events.Remove(eventEntity);
    }
}
