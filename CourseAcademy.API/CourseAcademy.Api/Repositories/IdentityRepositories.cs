using CourseAcademy.Api.Common;
using CourseAcademy.Api.Data;
using CourseAcademy.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Api.Repositories;

public interface IUserRepository : IRepository<User>
{
    /// <summary>Tracked user with role, for sign-in. <paramref name="email"/> must already be normalised.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);

    Task<bool> EmailExistsAsync(string email, CancellationToken ct);

    /// <summary>Tracked user with everything that decides access: role screens, own screens and course grants.</summary>
    Task<User?> GetWithAccessAsync(int id, CancellationToken ct);

    /// <summary>Users whose name or email contains <paramref name="search"/>, oldest first, with role, own screens and course grants.</summary>
    Task<(IReadOnlyList<User> Items, int Total)> SearchAsync(string? search, PageQuery page, CancellationToken ct);

    Task<bool> AnyWithRoleAsync(int roleId, CancellationToken ct);

    /// <summary>Number of users per role id.</summary>
    Task<IReadOnlyDictionary<int, int>> CountByRoleAsync(CancellationToken ct);
}

public sealed class UserRepository(AppDbContext db) : Repository<User>(db), IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        Set.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct) => Set.AnyAsync(u => u.Email == email, ct);

    public Task<User?> GetWithAccessAsync(int id, CancellationToken ct) =>
        Set.Include(u => u.Role).ThenInclude(r => r.Screens)
            .Include(u => u.Screens)
            .Include(u => u.CourseGrants).ThenInclude(g => g.Permissions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<(IReadOnlyList<User> Items, int Total)> SearchAsync(string? search, PageQuery page, CancellationToken ct)
    {
        var query = Query;
        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(u => u.Name.Contains(term) || u.Email.Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .Include(u => u.Role)
            .Include(u => u.Screens)
            .Include(u => u.CourseGrants).ThenInclude(g => g.Permissions)
            .OrderBy(u => u.Id)
            .Skip(page.Skip).Take(page.PageSize)
            .AsSplitQuery()
            .ToListAsync(ct);
        return (items, total);
    }

    public Task<bool> AnyWithRoleAsync(int roleId, CancellationToken ct) => Set.AnyAsync(u => u.RoleId == roleId, ct);

    public async Task<IReadOnlyDictionary<int, int>> CountByRoleAsync(CancellationToken ct) =>
        await Query.GroupBy(u => u.RoleId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
}

public interface IRoleRepository : IRepository<Role>
{
    Task<IReadOnlyList<Role>> ListWithScreensAsync(CancellationToken ct);

    /// <summary>Tracked role with its screens.</summary>
    Task<Role?> GetByCodeAsync(string code, CancellationToken ct);

    Task<bool> NameExistsAsync(string name, int? exceptId, CancellationToken ct);
}

public sealed class RoleRepository(AppDbContext db) : Repository<Role>(db), IRoleRepository
{
    public async Task<IReadOnlyList<Role>> ListWithScreensAsync(CancellationToken ct) =>
        await Query.Include(r => r.Screens).OrderBy(r => r.Id).ToListAsync(ct);

    public Task<Role?> GetByCodeAsync(string code, CancellationToken ct) =>
        Set.Include(r => r.Screens).FirstOrDefaultAsync(r => r.Code == code, ct);

    public Task<bool> NameExistsAsync(string name, int? exceptId, CancellationToken ct)
    {
        var lower = name.ToLower();
        return Set.AnyAsync(r => r.Name.ToLower() == lower && r.Id != exceptId, ct);
    }
}

public interface IScreenRepository
{
    /// <summary>Every screen with its permissions, in sidebar order.</summary>
    Task<IReadOnlyList<Screen>> ListWithPermissionsAsync(CancellationToken ct);
}

public sealed class ScreenRepository(AppDbContext db) : IScreenRepository
{
    public async Task<IReadOnlyList<Screen>> ListWithPermissionsAsync(CancellationToken ct) =>
        await db.Screens.AsNoTracking().Include(s => s.Permissions).OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToListAsync(ct);
}

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    /// <summary>Tracked token with its user and role. Null when the token is unknown or the user was deleted.</summary>
    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct);

    /// <summary>Tracked tokens of a user that are not revoked and not expired.</summary>
    Task<List<RefreshToken>> ListActiveAsync(int userId, DateTime now, CancellationToken ct);

    /// <summary>Tracked tokens of a user that expired before <paramref name="now"/>.</summary>
    Task<List<RefreshToken>> ListExpiredAsync(int userId, DateTime now, CancellationToken ct);
}

public sealed class RefreshTokenRepository(AppDbContext db) : Repository<RefreshToken>(db), IRefreshTokenRepository
{
    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct) =>
        Set.Include(t => t.User).ThenInclude(u => u.Role).FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public Task<List<RefreshToken>> ListActiveAsync(int userId, DateTime now, CancellationToken ct) =>
        Set.Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now).ToListAsync(ct);

    public Task<List<RefreshToken>> ListExpiredAsync(int userId, DateTime now, CancellationToken ct) =>
        Set.Where(t => t.UserId == userId && t.ExpiresAt <= now).ToListAsync(ct);
}
