using Application.Common.Interfaces;
using Application.Dtos.Events;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _db;

    public EventRepository(AppDbContext db) => _db = db;

    public Task<Event?> GetByIdAsync(Guid id) => _db.Events.FirstOrDefaultAsync(e => e.Id == id);

    public async Task<(IReadOnlyList<Event> Items, int TotalCount)> ListAsync(Guid? ownerId, EventQuery query)
    {
        IQueryable<Event> events = _db.Events.AsNoTracking();

        if (ownerId.HasValue)
        {
            events = events.Where(e => e.OwnerId == ownerId.Value);
        }

        if (query.Status.HasValue)
        {
            events = events.Where(e => e.Status == query.Status.Value);
        }

        if (query.EventType.HasValue)
        {
            events = events.Where(e => e.EventType == query.EventType.Value);
        }

        var descending = string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        events = query.SortBy?.ToLowerInvariant() switch
        {
            "preferreddate" => descending ? events.OrderByDescending(e => e.PreferredDate) : events.OrderBy(e => e.PreferredDate),
            "budget" => descending ? events.OrderByDescending(e => e.Budget) : events.OrderBy(e => e.Budget),
            "guestcount" => descending ? events.OrderByDescending(e => e.GuestCount) : events.OrderBy(e => e.GuestCount),
            _ => descending ? events.OrderByDescending(e => e.CreatedAt) : events.OrderBy(e => e.CreatedAt)
        };

        var totalCount = await events.CountAsync();
        var items = await events
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(Event eventEntity) => await _db.Events.AddAsync(eventEntity);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    public void Remove(Event eventEntity) => _db.Events.Remove(eventEntity);
}
