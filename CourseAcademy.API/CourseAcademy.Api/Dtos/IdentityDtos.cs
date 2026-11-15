namespace CourseAcademy.Api.Dtos;

// ── Auth ────────────────────────────────────────────────────────

public sealed record RegisterRequest(string Name, string Email, string Password, string? Phone, bool AcceptTerms);

public sealed record LoginRequest(string Email, string Password, bool Remember = true);

public sealed record RefreshRequest(string RefreshToken);

public sealed record LogoutRequest(string? RefreshToken);

public sealed record CourseGrantDto(int CourseId, IReadOnlyList<string> Permissions);

/// <summary>The signed-in user and everything the frontend needs to decide what to show.</summary>
public sealed record MeDto(
    int Id, string Name, string Email, string? Phone, string RoleCode, string RoleName,
    IReadOnlyList<string> Permissions, IReadOnlyList<CourseGrantDto> CourseGrants,
    bool HasAdminPanel, string HomeUrl, DateTime CreatedAt);

public sealed record TokensDto(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken);

public sealed record AuthResultDto(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, MeDto Me);

// ── Screens and roles ───────────────────────────────────────────

public sealed record ScreenDto(
    string Code, string Label, string Area, string Path, string Icon, bool IsCourseScoped, string? Section,
    IReadOnlyList<string> Permissions);

public sealed record RoleDto(
    string Code, string Name, string Description, bool IsSystem,
    IReadOnlyList<string> Screens, IReadOnlyList<string> Permissions, int MemberCount);

public sealed record RoleSaveRequest(string Name, string? Description, IReadOnlyList<string>? Screens);

// ── Users and their access ──────────────────────────────────────

public sealed record UserSummaryDto(int Id, string Name, string Email);

public static class AccessTypes
{
    /// <summary>Admin role: every screen, cannot be reduced.</summary>
    public const string Full = "full";
    /// <summary>The user's own screen list replaces the role's.</summary>
    public const string Custom = "custom";
    /// <summary>The role's screens.</summary>
    public const string Role = "role";
}

/// <summary>
/// A row of the users list. AccessType is one of <see cref="AccessTypes"/>; CustomScreens holds the user's own
/// screens when AccessType is "custom", otherwise null.
/// </summary>
public sealed record UserListItemDto(
    int Id, string Name, string Email, string? Phone, string RoleCode, string RoleName,
    string AccessType, IReadOnlyList<string>? CustomScreens, IReadOnlyList<CourseGrantDto> CourseGrants, DateTime CreatedAt);

public sealed record SetCourseGrantRequest(IReadOnlyList<string> Permissions);

/// <summary>One user's access. LockReason says why the caller cannot change it, or is null when they can.</summary>
public sealed record UserAccessDto(
    UserSummaryDto User, string RoleCode, string AccessType,
    IReadOnlyList<string> Screens, IReadOnlyList<string> Permissions, IReadOnlyList<CourseGrantDto> CourseGrants,
    bool CanEdit, string? LockReason);

public sealed record SetUserRoleRequest(string RoleCode);

public sealed record SetUserAccessRequest(IReadOnlyList<string> Screens);

public sealed class UserListQuery : Common.PageQuery
{
    public string? Search { get; set; }
}
