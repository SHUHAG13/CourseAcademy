using CourseAcademy.Api.Data;
using CourseAcademy.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Api.Repositories;

/// <summary>Reads lookup tables in display order.</summary>
public interface ILookupRepository
{
    Task<IReadOnlyList<T>> ListAsync<T>(CancellationToken ct) where T : LookupEntity;
    Task<T?> FindByCodeAsync<T>(string code, CancellationToken ct) where T : LookupEntity;
}

public sealed class LookupRepository(AppDbContext db) : ILookupRepository
{
    public async Task<IReadOnlyList<T>> ListAsync<T>(CancellationToken ct) where T : LookupEntity =>
        await db.Set<T>().AsNoTracking().OrderBy(e => e.SortOrder).ThenBy(e => e.Id).ToListAsync(ct);

    public Task<T?> FindByCodeAsync<T>(string code, CancellationToken ct) where T : LookupEntity =>
        db.Set<T>().AsNoTracking().FirstOrDefaultAsync(e => e.Code == code, ct);
}
