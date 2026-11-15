using System.Linq.Expressions;
using CourseAcademy.Api.Data;
using CourseAcademy.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Api.Repositories;

/// <summary>
/// Basic data access for one entity type. Changes are tracked and only written by <see cref="IUnitOfWork.SaveChangesAsync"/>.
/// Soft-deleted rows are never returned.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<T>> ListAsync(CancellationToken ct);
    Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken ct);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct);
    Task AddAsync(T entity, CancellationToken ct);
    void Update(T entity);

    /// <summary>Soft-deletes the entity, or removes it when it implements <see cref="IHardDelete"/>.</summary>
    void Remove(T entity);
}

public class Repository<T>(AppDbContext db) : IRepository<T> where T : BaseEntity
{
    protected AppDbContext Db { get; } = db;
    protected DbSet<T> Set { get; } = db.Set<T>();

    /// <summary>Read-only query for derived repositories. Use tracking queries only when the result will be changed.</summary>
    protected IQueryable<T> Query => Set.AsNoTracking();

    public virtual Task<T?> GetByIdAsync(int id, CancellationToken ct) => Set.FirstOrDefaultAsync(e => e.Id == id, ct);

    public virtual async Task<IReadOnlyList<T>> ListAsync(CancellationToken ct) => await Query.OrderBy(e => e.Id).ToListAsync(ct);

    public virtual async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken ct) =>
        await Query.Where(predicate).OrderBy(e => e.Id).ToListAsync(ct);

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct) => Set.AnyAsync(predicate, ct);

    public Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct) => Set.CountAsync(predicate, ct);

    public async Task AddAsync(T entity, CancellationToken ct) => await Set.AddAsync(entity, ct);

    public void Update(T entity) => Set.Update(entity);

    public void Remove(T entity)
    {
        if (entity is IHardDelete)
        {
            Set.Remove(entity);
            return;
        }
        // Mark the row instead of deleting it, so EF never tries to cut its children loose.
        if (Db.Entry(entity).State == EntityState.Detached) Set.Attach(entity);
        entity.IsDeleted = true;
    }
}
