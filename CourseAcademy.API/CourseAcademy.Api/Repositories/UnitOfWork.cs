using CourseAcademy.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Api.Repositories;

/// <summary>Commits the changes collected by repositories, optionally inside one database transaction.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);

    /// <summary>
    /// Runs <paramref name="work"/> in a transaction and commits it. The whole unit is retried on transient
    /// SQL errors, so <paramref name="work"/> must be safe to run again (no external side effects).
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken ct);
}

public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async token =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(token);
            try
            {
                var result = await work(token);
                await db.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
                return result;
            }
            catch
            {
                // A retry starts from a clean change tracker.
                db.ChangeTracker.Clear();
                throw;
            }
        }, ct);
    }
}
