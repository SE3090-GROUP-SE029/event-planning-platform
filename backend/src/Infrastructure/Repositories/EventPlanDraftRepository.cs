using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class EventPlanDraftRepository(AppDbContext db) : IEventPlanDraftRepository
{
    public Task<EventPlanDraft?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.EventPlanDrafts
            .Include(plan => plan.Event)
            .FirstOrDefaultAsync(plan => plan.Id == id, cancellationToken);

    public async Task<IReadOnlyList<EventPlanDraft>> ListAsync(
        Guid? eventId,
        PlanStatus? status,
        int? version,
        CancellationToken cancellationToken = default)
    {
        var query = db.EventPlanDrafts.AsNoTracking().AsQueryable();
        if (eventId.HasValue) query = query.Where(plan => plan.EventId == eventId.Value);
        if (status.HasValue) query = query.Where(plan => plan.Status == status.Value);
        if (version.HasValue) query = query.Where(plan => plan.Version == version.Value);
        return await query.OrderByDescending(plan => plan.Version).ToListAsync(cancellationToken);
    }

    public Task<int> GetNextVersionAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        db.EventPlanDrafts
            .Where(plan => plan.EventId == eventId)
            .Select(plan => (int?)plan.Version)
            .MaxAsync(cancellationToken)
            .ContinueWith(task => (task.Result ?? 0) + 1, cancellationToken);

    public async Task AddAsync(EventPlanDraft plan, CancellationToken cancellationToken = default) =>
        await db.EventPlanDrafts.AddAsync(plan, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
