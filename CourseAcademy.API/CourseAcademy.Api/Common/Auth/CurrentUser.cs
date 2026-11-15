using System.Security.Claims;
using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Repositories;
using CourseAcademy.Api.Services;
using Microsoft.IdentityModel.JsonWebTokens;

namespace CourseAcademy.Api.Common.Auth;

/// <summary>The signed-in user of the current request.</summary>
public interface ICurrentUser
{
    /// <summary>Null for anonymous requests.</summary>
    int? UserId { get; }

    /// <summary>The user id, or a 401 when nobody is signed in.</summary>
    int RequireUserId();

    /// <summary>
    /// The user's access, read from the database once per request so role and access changes apply immediately.
    /// Fails with 401 when nobody is signed in or the account no longer exists.
    /// </summary>
    Task<UserAccess> GetAccessAsync(CancellationToken ct);

    /// <summary>The user's access, or null for anonymous visitors and deleted accounts. For public endpoints.</summary>
    Task<UserAccess?> TryGetAccessAsync(CancellationToken ct);

    /// <summary>Fails with 403 unless the user has the permission globally or on <paramref name="courseId"/>.</summary>
    Task EnsureCanAsync(string permission, int? courseId, CancellationToken ct);
}

public sealed class CurrentUser(IHttpContextAccessor http, IUserRepository users, IScreenCatalog catalog) : ICurrentUser
{
    private UserAccess? _access;

    public int? UserId
    {
        get
        {
            var principal = http.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true) return null;
            var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public int RequireUserId() => UserId ?? throw new UnauthorizedException();

    public async Task<UserAccess> GetAccessAsync(CancellationToken ct)
    {
        if (_access is not null) return _access;
        var user = await users.GetWithAccessAsync(RequireUserId(), ct) ?? throw new UnauthorizedException();
        return _access = new UserAccess(user, await catalog.GetAsync(ct));
    }

    public async Task<UserAccess?> TryGetAccessAsync(CancellationToken ct)
    {
        if (_access is not null) return _access;
        if (UserId is not { } id) return null;
        var user = await users.GetWithAccessAsync(id, ct);
        return user is null ? null : _access = new UserAccess(user, await catalog.GetAsync(ct));
    }

    public async Task EnsureCanAsync(string permission, int? courseId, CancellationToken ct)
    {
        if (!(await GetAccessAsync(ct)).Can(permission, courseId)) throw new ForbiddenException();
    }
}
