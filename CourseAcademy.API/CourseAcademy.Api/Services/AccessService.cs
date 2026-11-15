using CourseAcademy.Api.Common;
using CourseAcademy.Api.Common.Auth;
using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Repositories;

namespace CourseAcademy.Api.Services;

/// <summary>Admin management of roles, each user's screen access and per-course grants.</summary>
public interface IAccessService
{
    Task<IReadOnlyList<ScreenDto>> GetScreensAsync(CancellationToken ct);
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct);
    Task<RoleDto> CreateRoleAsync(RoleSaveRequest request, CancellationToken ct);
    Task<RoleDto> UpdateRoleAsync(string code, RoleSaveRequest request, CancellationToken ct);
    Task DeleteRoleAsync(string code, CancellationToken ct);
    Task<PagedResult<UserListItemDto>> GetUsersAsync(UserListQuery query, CancellationToken ct);
    Task<UserAccessDto> GetUserAccessAsync(int userId, CancellationToken ct);
    Task<UserAccessDto> SetUserRoleAsync(int userId, SetUserRoleRequest request, CancellationToken ct);
    Task<UserAccessDto> SetUserAccessAsync(int userId, SetUserAccessRequest request, CancellationToken ct);
    Task<UserAccessDto> ResetUserAccessAsync(int userId, CancellationToken ct);
    Task<UserAccessDto> SetCourseGrantAsync(int userId, int courseId, SetCourseGrantRequest request, CancellationToken ct);
}

public sealed class AccessService(
    IUserRepository users,
    IRoleRepository roles,
    IRepository<RoleScreen> roleScreens,
    IRepository<UserScreen> userScreens,
    IRepository<CourseGrant> courseGrants,
    IRepository<CourseGrantPermission> grantPermissions,
    IUnitOfWork unitOfWork,
    IScreenCatalog catalog,
    ICurrentUser currentUser) : IAccessService
{
    // ── Screens and roles ───────────────────────────────────────

    public async Task<IReadOnlyList<ScreenDto>> GetScreensAsync(CancellationToken ct)
    {
        await EnsureAnyAsync(ct, PermissionCodes.RolesView, PermissionCodes.AccessView);
        var data = await catalog.GetAsync(ct);
        return data.Screens
            .Select(s => new ScreenDto(s.Code, s.Name, s.Area, s.Path, s.Icon, s.IsCourseScoped, s.Section, s.Permissions))
            .ToList();
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct)
    {
        // The users list shows role names, so anyone who can see users can see roles.
        await EnsureAnyAsync(ct, PermissionCodes.RolesView, PermissionCodes.AccessView, PermissionCodes.UsersView);
        var data = await catalog.GetAsync(ct);
        var counts = await users.CountByRoleAsync(ct);
        return (await roles.ListWithScreensAsync(ct)).Select(r => ToDto(r, data, counts.GetValueOrDefault(r.Id))).ToList();
    }

    public async Task<RoleDto> CreateRoleAsync(RoleSaveRequest request, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.RolesEdit, null, ct);
        var data = await catalog.GetAsync(ct);
        var name = request.Name.Trim();
        if (await roles.NameExistsAsync(name, null, ct)) throw new ConflictException("A role with this name already exists.");

        var role = new Role
        {
            Code = $"role-{Guid.NewGuid():N}"[..13],
            Name = name,
            Description = Clean(request.Description),
            IsSystem = false,
        };
        foreach (var screenId in ScreenIds(request.Screens, data)) role.Screens.Add(new RoleScreen { ScreenId = screenId });
        await roles.AddAsync(role, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(role, data, 0);
    }

    public async Task<RoleDto> UpdateRoleAsync(string code, RoleSaveRequest request, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.RolesEdit, null, ct);
        var data = await catalog.GetAsync(ct);
        var role = await roles.GetByCodeAsync(code, ct) ?? throw new NotFoundException("Role not found.");
        var name = request.Name.Trim();
        if (await roles.NameExistsAsync(name, role.Id, ct)) throw new ConflictException("A role with this name already exists.");

        role.Name = name;
        role.Description = Clean(request.Description);
        // The admin role always has every screen, so its screen list is not stored (BR-ACL-05).
        if (role.Code != Role.AdminCode)
        {
            var wanted = ScreenIds(request.Screens, data).ToHashSet();
            foreach (var existing in role.Screens.Where(s => !wanted.Contains(s.ScreenId)).ToList()) roleScreens.Remove(existing);
            foreach (var screenId in wanted.Where(id => role.Screens.All(s => s.ScreenId != id))) role.Screens.Add(new RoleScreen { ScreenId = screenId });
        }
        await unitOfWork.SaveChangesAsync(ct);

        var members = (await users.CountByRoleAsync(ct)).GetValueOrDefault(role.Id);
        return ToDto(role, data, members);
    }

    public async Task DeleteRoleAsync(string code, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.RolesEdit, null, ct);
        var role = await roles.GetByCodeAsync(code, ct) ?? throw new NotFoundException("Role not found.");
        if (role.IsSystem) throw new ConflictException("Built-in roles cannot be deleted.");
        if (await users.AnyWithRoleAsync(role.Id, ct)) throw new ConflictException("Move the users with this role to another role first.");

        foreach (var screen in role.Screens.ToList()) roleScreens.Remove(screen);
        roles.Remove(role);
        await unitOfWork.SaveChangesAsync(ct);
    }

    // ── Users ───────────────────────────────────────────────────

    public async Task<PagedResult<UserListItemDto>> GetUsersAsync(UserListQuery query, CancellationToken ct)
    {
        await EnsureAnyAsync(ct, PermissionCodes.UsersView, PermissionCodes.AccessView);
        var data = await catalog.GetAsync(ct);
        var (items, total) = await users.SearchAsync(query.Search, query, ct);
        var list = items.Select(u =>
        {
            var type = AccessType(u);
            return new UserListItemDto(
                u.Id, u.Name, u.Email, u.Phone, u.Role.Code, u.Role.Name, type,
                type == AccessTypes.Custom ? data.CodesOf(u.Screens.Select(s => s.ScreenId)) : null,
                u.CourseGrants
                    .Select(g => new CourseGrantDto(g.CourseId, g.Permissions.Select(p => data.PermissionCodeById[p.PermissionId]).Order(StringComparer.Ordinal).ToList()))
                    .OrderBy(g => g.CourseId)
                    .ToList(),
                u.CreatedAt);
        }).ToList();
        return new PagedResult<UserListItemDto>(list, total, query.Page, query.PageSize);
    }

    public async Task<UserAccessDto> GetUserAccessAsync(int userId, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.AccessView, null, ct);
        var user = await users.GetWithAccessAsync(userId, ct) ?? throw new NotFoundException("User not found.");
        return await ToAccessDtoAsync(user, ct);
    }

    public async Task<UserAccessDto> SetUserRoleAsync(int userId, SetUserRoleRequest request, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.RolesEdit, null, ct);
        var user = await users.GetWithAccessAsync(userId, ct) ?? throw new NotFoundException("User not found.");
        if (user.Id == currentUser.RequireUserId()) throw new ConflictException("You can't change your own role.");
        var role = await roles.GetByCodeAsync(request.RoleCode, ct)
            ?? throw new BusinessRuleException("Role not found.", "roleCode");

        user.RoleId = role.Id;
        user.Role = role;
        await unitOfWork.SaveChangesAsync(ct);
        return await ToAccessDtoAsync(user, ct);
    }

    public async Task<UserAccessDto> SetUserAccessAsync(int userId, SetUserAccessRequest request, CancellationToken ct)
    {
        var user = await EditableUserAsync(userId, ct);
        var data = await catalog.GetAsync(ct);
        var wanted = ScreenIds(request.Screens, data).ToHashSet();

        foreach (var existing in user.Screens.Where(s => !wanted.Contains(s.ScreenId)).ToList()) userScreens.Remove(existing);
        foreach (var screenId in wanted.Where(id => user.Screens.All(s => s.ScreenId != id))) user.Screens.Add(new UserScreen { ScreenId = screenId });
        user.HasCustomAccess = true;
        await unitOfWork.SaveChangesAsync(ct);
        return await ToAccessDtoAsync(user, ct);
    }

    public async Task<UserAccessDto> ResetUserAccessAsync(int userId, CancellationToken ct)
    {
        var user = await EditableUserAsync(userId, ct);
        foreach (var existing in user.Screens.ToList()) userScreens.Remove(existing);
        user.HasCustomAccess = false;
        await unitOfWork.SaveChangesAsync(ct);
        return await ToAccessDtoAsync(user, ct);
    }

    public async Task<UserAccessDto> SetCourseGrantAsync(int userId, int courseId, SetCourseGrantRequest request, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.AccessEdit, null, ct);
        if (courseId <= 0) throw new NotFoundException("Course not found.");
        var user = await users.GetWithAccessAsync(userId, ct) ?? throw new NotFoundException("User not found.");
        var data = await catalog.GetAsync(ct);

        var unknown = request.Permissions.FirstOrDefault(p => !data.CourseGrantable.Contains(p));
        if (unknown is not null) throw new BusinessRuleException($"\"{unknown}\" cannot be granted for one course.", "permissions");
        var wanted = request.Permissions.Distinct().Select(p => data.PermissionIdByCode[p]).ToHashSet();

        var grant = user.CourseGrants.FirstOrDefault(g => g.CourseId == courseId);
        if (wanted.Count == 0)
        {
            // An empty list removes the grant (BR-ACL-13).
            if (grant is not null)
            {
                foreach (var permission in grant.Permissions.ToList()) grantPermissions.Remove(permission);
                courseGrants.Remove(grant);
                user.CourseGrants.Remove(grant);
            }
        }
        else
        {
            if (grant is null)
            {
                grant = new CourseGrant { UserId = user.Id, CourseId = courseId };
                user.CourseGrants.Add(grant);
            }
            foreach (var existing in grant.Permissions.Where(p => !wanted.Contains(p.PermissionId)).ToList())
            {
                grantPermissions.Remove(existing);
                grant.Permissions.Remove(existing);
            }
            foreach (var permissionId in wanted.Where(id => grant.Permissions.All(p => p.PermissionId != id)))
            {
                grant.Permissions.Add(new CourseGrantPermission { PermissionId = permissionId });
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
        return await ToAccessDtoAsync(user, ct);
    }

    // ── Helpers ─────────────────────────────────────────────────

    /// <summary>Admins always keep full access and nobody edits their own, so nobody can lock everyone out.</summary>
    private async Task<User> EditableUserAsync(int userId, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.AccessEdit, null, ct);
        var user = await users.GetWithAccessAsync(userId, ct) ?? throw new NotFoundException("User not found.");
        if (user.Id == currentUser.RequireUserId()) throw new ConflictException("You can't change your own access.");
        if (user.Role.Code == Role.AdminCode) throw new ConflictException("Admin users always have full access.");
        return user;
    }

    private async Task<UserAccessDto> ToAccessDtoAsync(User user, CancellationToken ct)
    {
        var data = await catalog.GetAsync(ct);
        var access = new UserAccess(user, data);
        var caller = await currentUser.GetAccessAsync(ct);
        string? lockReason =
            access.IsAdmin ? "Admin users always have full access." :
            user.Id == caller.UserId ? "You can't change your own access." :
            !caller.Can(PermissionCodes.AccessEdit) ? "You can view this access but not change it." :
            null;

        return new UserAccessDto(
            new UserSummaryDto(user.Id, user.Name, user.Email),
            user.Role.Code,
            AccessType(user),
            access.ScreenCodes,
            access.Permissions,
            access.CourseGrants.Select(g => new CourseGrantDto(g.Key, g.Value.Order(StringComparer.Ordinal).ToList())).OrderBy(g => g.CourseId).ToList(),
            lockReason is null,
            lockReason);
    }

    private static string AccessType(User user) =>
        user.Role.Code == Role.AdminCode ? AccessTypes.Full : user.HasCustomAccess ? AccessTypes.Custom : AccessTypes.Role;

    private static RoleDto ToDto(Role role, ScreenCatalogData data, int members)
    {
        var screenIds = role.Code == Role.AdminCode ? data.Screens.Select(s => s.Id) : role.Screens.Select(s => s.ScreenId);
        var ids = screenIds.ToList();
        return new RoleDto(role.Code, role.Name, role.Description ?? string.Empty, role.IsSystem,
            data.CodesOf(ids), role.Code == Role.AdminCode ? data.AllPermissions : data.PermissionsOf(ids), members);
    }

    private static IEnumerable<int> ScreenIds(IEnumerable<string>? codes, ScreenCatalogData data)
    {
        var ids = new List<int>();
        foreach (var code in (codes ?? []).Distinct())
        {
            if (!data.ByCode.TryGetValue(code, out var screen)) throw new BusinessRuleException($"Unknown screen \"{code}\".", "screens");
            ids.Add(screen.Id);
        }
        return ids;
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private async Task EnsureAnyAsync(CancellationToken ct, params string[] permissions)
    {
        var access = await currentUser.GetAccessAsync(ct);
        if (!permissions.Any(p => access.Can(p))) throw new ForbiddenException();
    }
}
