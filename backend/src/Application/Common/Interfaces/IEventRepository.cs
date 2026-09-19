using Domain.Entities;
using Application.Dtos.Events;

namespace Application.Common.Interfaces;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id);
    Task<(IReadOnlyList<Event> Items, int TotalCount)> ListAsync(Guid? ownerId, EventQuery query);
    Task AddAsync(Event eventEntity);
    Task SaveChangesAsync();
    void Remove(Event eventEntity);
}
