using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace CourseAcademy.Api.Services;

/// <summary>One screen and the codes of its permissions, as read from the database.</summary>
public sealed record ScreenInfo(
    int Id, string Code, string Name, string Area, string Path, string Icon, bool IsCourseScoped, string? Section,
    string ViewPermission, IReadOnlyList<string> Permissions);

/// <summary>The fixed list of screens and permissions, with the look-ups the access rules need.</summary>
public sealed class ScreenCatalogData
{
    public ScreenCatalogData(IReadOnlyList<Screen> screens)
    {
        Screens = screens.Select(s => new ScreenInfo(
            s.Id, s.Code, s.Name, s.Area, s.Path, s.Icon, s.IsCourseScoped, s.Section,
            s.Permissions.OrderBy(p => p.SortOrder).First(p => p.Action == Permission.ViewAction).Code,
            s.Permissions.OrderBy(p => p.SortOrder).Select(p => p.Code).ToList())).ToList();
        ByCode = Screens.ToDictionary(s => s.Code);
        ById = Screens.ToDictionary(s => s.Id);
        var permissions = screens.SelectMany(s => s.Permissions).OrderBy(p => p.SortOrder).ToList();
        AllPermissions = permissions.Select(p => p.Code).ToList();
        PermissionCodeById = permissions.ToDictionary(p => p.Id, p => p.Code);
        PermissionIdByCode = permissions.ToDictionary(p => p.Code, p => p.Id);
        CourseGrantable = permissions.Where(p => p.IsCourseGrantable).Select(p => p.Code).ToHashSet();
    }

    /// <summary>In sidebar order.</summary>
    public IReadOnlyList<ScreenInfo> Screens { get; }
    public IReadOnlyDictionary<string, ScreenInfo> ByCode { get; }
    public IReadOnlyDictionary<int, ScreenInfo> ById { get; }
    public IReadOnlyList<string> AllPermissions { get; }
    public IReadOnlyDictionary<int, string> PermissionCodeById { get; }
    public IReadOnlyDictionary<string, int> PermissionIdByCode { get; }
    public IReadOnlySet<string> CourseGrantable { get; }

    /// <summary>Every permission of the given screens, in catalogue order.</summary>
    public IReadOnlyList<string> PermissionsOf(IEnumerable<int> screenIds)
    {
        var ids = screenIds.ToHashSet();
        return Screens.Where(s => ids.Contains(s.Id)).SelectMany(s => s.Permissions).ToList();
    }

    /// <summary>Codes of the given screens, in catalogue order.</summary>
    public IReadOnlyList<string> CodesOf(IEnumerable<int> screenIds)
    {
        var ids = screenIds.ToHashSet();
        return Screens.Where(s => ids.Contains(s.Id)).Select(s => s.Code).ToList();
    }
}

public interface IScreenCatalog
{
    Task<ScreenCatalogData> GetAsync(CancellationToken ct);
}

/// <summary>Screens and permissions only change with a migration, so they are read once and cached for the process.</summary>
public sealed class ScreenCatalog(IScreenRepository screens, IMemoryCache cache) : IScreenCatalog
{
    private const string CacheKey = "screen-catalog";

    public async Task<ScreenCatalogData> GetAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(CacheKey, out ScreenCatalogData? cached) && cached is not null) return cached;
        var data = new ScreenCatalogData(await screens.ListWithPermissionsAsync(ct));
        cache.Set(CacheKey, data);
        return data;
    }
}
