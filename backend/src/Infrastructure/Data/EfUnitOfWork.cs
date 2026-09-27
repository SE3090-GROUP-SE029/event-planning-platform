using Application.Common.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public EfUnitOfWork(AppDbContext db) => _db = db;

    public async Task ExecuteInTransactionAsync(Func<Task> action)
    {
        if (!_db.Database.IsRelational())
        {
            await action();
            return;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            await action();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
