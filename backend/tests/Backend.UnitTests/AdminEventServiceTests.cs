using Application.Common.Interfaces;
using Application.Dtos.Events;
using Application.Services.Events;
using Domain.Entities;
using Domain.Enums;

namespace Backend.UnitTests;

public class AdminEventServiceTests
{
    [Fact]
    public async Task ListAsync_returns_all_events_with_owner_details()
    {
        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@example.com",
            FirstName = "Event",
            LastName = "Owner"
        };
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            OwnerId = owner.Id,
            EventType = EventType.WEDDING,
            GuestCount = 100,
            Budget = 5000,
            PreferredVenue = "Hall",
            PreferredDate = DateTime.UtcNow.AddDays(10),
            EventDuration = TimeSpan.FromHours(4),
            Status = EventStatus.DRAFT,
            CreatedAt = DateTime.UtcNow
        };
        var events = new TestEventRepository(eventEntity);
        var service = new AdminEventService(events, new TestUserRepository(owner));

        var result = await service.ListAsync(new AdminEventQuery());

        Assert.Single(result.Items);
        Assert.Equal(owner.Email, result.Items[0].Owner!.Email);
        Assert.Equal(eventEntity.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task ListAsync_rejects_invalid_sort_field()
    {
        var service = new AdminEventService(new TestEventRepository(), new TestUserRepository());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ListAsync(new AdminEventQuery { SortBy = "owner" }));
    }

    private sealed class TestEventRepository(params Event[] events) : IEventRepository
    {
        private readonly List<Event> _events = events.ToList();
        public Task<Event?> GetByIdAsync(Guid id) => Task.FromResult(_events.SingleOrDefault(e => e.Id == id));
        public Task<(IReadOnlyList<Event> Items, int TotalCount)> ListAsync(Guid? ownerId, EventQuery query) =>
            Task.FromResult(((IReadOnlyList<Event>)_events, _events.Count));
        public Task<(IReadOnlyList<Event> Items, int TotalCount)> ListAdminAsync(AdminEventQuery query) =>
            Task.FromResult(((IReadOnlyList<Event>)_events, _events.Count));
        public Task AddAsync(Event eventEntity) => Task.CompletedTask;
        public Task SaveChangesAsync() => Task.CompletedTask;
        public void Remove(Event eventEntity) { }
    }

    private sealed class TestUserRepository(params User[] users) : IUserRepository
    {
        private readonly List<User> _users = users.ToList();
        public Task<User?> GetByEmailAsync(string email) => Task.FromResult<User?>(null);
        public Task<User?> GetByIdWithRoleAsync(Guid id) => Task.FromResult(_users.SingleOrDefault(u => u.Id == id));
        public Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<Guid> ids) =>
            Task.FromResult((IReadOnlyList<User>)_users.Where(u => ids.Contains(u.Id)).ToList());
        public Task<Role?> GetRoleByNameAsync(RoleName roleName) => Task.FromResult<Role?>(null);
        public Task AddAsync(User user) => Task.CompletedTask;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
