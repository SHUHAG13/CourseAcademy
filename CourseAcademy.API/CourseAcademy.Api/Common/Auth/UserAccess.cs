using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Services;

namespace CourseAcademy.Api.Common.Auth;

/// <summary>
/// What one user may do. Global permissions come from the admin role (everything), or the user's own
/// screens if an admin set them, or else the role's screens. Course grants add permissions on single courses.
/// </summary>
public sealed class UserAccess
{
    private readonly HashSet<string> _permissions;
    private readonly Dictionary<int, HashSet<string>> _grants;
    private readonly ScreenCatalogData _catalog;

    public UserAccess(User user, ScreenCatalogData catalog)
    {
        _catalog = catalog;
        UserId = user.Id;
        RoleCode = user.Role.Code;
        IsAdmin = user.Role.Code == Role.AdminCode;
        HasCustomAccess = !IsAdmin && user.HasCustomAccess;

        var screenIds = IsAdmin
            ? catalog.Screens.Select(s => s.Id)
            : HasCustomAccess ? user.Screens.Select(s => s.ScreenId) : user.Role.Screens.Select(s => s.ScreenId);
        ScreenCodes = catalog.CodesOf(screenIds);
        Permissions = IsAdmin ? catalog.AllPermissions : catalog.PermissionsOf(ScreenCodes.Select(code => catalog.ByCode[code].Id));
        _permissions = Permissions.ToHashSet();

        _grants = user.CourseGrants.ToDictionary(
            g => g.CourseId,
            g => g.Permissions.Select(p => catalog.PermissionCodeById[p.PermissionId]).ToHashSet());
    }

    public int UserId { get; }
    public string RoleCode { get; }
    public bool IsAdmin { get; }
    public bool HasCustomAccess { get; }

    /// <summary>Screens the user has on every course, in catalogue order.</summary>
    public IReadOnlyList<string> ScreenCodes { get; }

    /// <summary>Permissions the user has on every course, in catalogue order.</summary>
    public IReadOnlyList<string> Permissions { get; }

    public IReadOnlyDictionary<int, HashSet<string>> CourseGrants => _grants;

    /// <summary>Whether the user has <paramref name="permission"/> globally, or through a grant on <paramref name="courseId"/>.</summary>
    public bool Can(string permission, int? courseId = null) =>
        _permissions.Contains(permission) ||
        (courseId is { } id && _grants.TryGetValue(id, out var granted) && granted.Contains(permission));

    /// <summary>Whether the user has <paramref name="permission"/> globally or on at least one course.</summary>
    public bool CanOnAnyCourse(string permission) =>
        _permissions.Contains(permission) || _grants.Values.Any(granted => granted.Contains(permission));

    public bool CanView(ScreenInfo screen) => screen.IsCourseScoped ? CanOnAnyCourse(screen.ViewPermission) : Can(screen.ViewPermission);

    private IEnumerable<ScreenInfo> AdminScreens => _catalog.Screens.Where(s => s.Area == Screen.AdminArea);

    /// <summary>True when the user can open any admin screen or has any course grant.</summary>
    public bool HasAdminPanel => AdminScreens.Any(s => _permissions.Contains(s.ViewPermission)) || _grants.Count > 0;

    /// <summary>The first page this user can open: an admin screen, else the dashboard, else the site home.</summary>
    public string HomeUrl
    {
        get
        {
            var first = AdminScreens.FirstOrDefault(CanView);
            if (first is not null) return first.Section is null ? first.Path : $"/admin/{first.Section}";
            return Can(PermissionCodes.DashboardView) ? "/dashboard" : "/";
        }
    }
}
