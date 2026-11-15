namespace CourseAcademy.Api.Entities;

/// <summary>A page access can be granted to. Access is given per whole screen: a screen gives all of its permissions.</summary>
public sealed class Screen : LookupEntity
{
    public const string AdminArea = "admin";
    public const string StudentArea = "student";

    public string Area { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;

    /// <summary>A grant on a single course also opens this screen, which then lists only those courses.</summary>
    public bool IsCourseScoped { get; set; }

    /// <summary>Screens in a section share one sidebar link, e.g. "security".</summary>
    public string? Section { get; set; }

    public ICollection<Permission> Permissions { get; set; } = [];
}

/// <summary>One action in one area, named <c>area.action</c>. Belongs to exactly one screen.</summary>
public sealed class Permission : LookupEntity
{
    public const string ViewAction = "view";

    public int ScreenId { get; set; }
    public Screen Screen { get; set; } = null!;

    /// <summary>"view", "create", "edit", "delete" or "extra". The "view" permission is what opens the screen.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Can be granted to a user for one course only.</summary>
    public bool IsCourseGrantable { get; set; }
}

public sealed class Role : BaseEntity
{
    public const string AdminCode = "admin";
    public const string InstructorCode = "instructor";
    /// <summary>Kept as "user" to match the frontend's student role id.</summary>
    public const string StudentCode = "user";

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Built-in roles can be edited but not deleted.</summary>
    public bool IsSystem { get; set; }

    /// <summary>Ignored for the admin role, which always has every screen.</summary>
    public ICollection<RoleScreen> Screens { get; set; } = [];
}

public sealed class RoleScreen : BaseEntity, IHardDelete
{
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public int ScreenId { get; set; }
    public Screen Screen { get; set; } = null!;
}

public sealed class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    /// <summary>Stored trimmed and lower-case.</summary>
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    /// <summary>When true, <see cref="Screens"/> replaces the role's screens for this user.</summary>
    public bool HasCustomAccess { get; set; }
    public ICollection<UserScreen> Screens { get; set; } = [];

    public ICollection<CourseGrant> CourseGrants { get; set; } = [];

    /// <summary>The instructor profile this account teaches as, if any.</summary>
    public int? InstructorId { get; set; }
    public Instructor? Instructor { get; set; }
}

public sealed class UserScreen : BaseEntity, IHardDelete
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int ScreenId { get; set; }
    public Screen Screen { get; set; } = null!;
}

/// <summary>Permissions one user has on one course only, e.g. an instructor on the courses they teach.</summary>
public sealed class CourseGrant : BaseEntity, IHardDelete
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public ICollection<CourseGrantPermission> Permissions { get; set; } = [];
}

public sealed class CourseGrantPermission : BaseEntity, IHardDelete
{
    public int CourseGrantId { get; set; }
    public CourseGrant CourseGrant { get; set; } = null!;
    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}

/// <summary>A refresh token. Only its hash is stored. Each token is used once and replaced (rotation).</summary>
public sealed class RefreshToken : BaseEntity, IHardDelete
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public int? ReplacedByTokenId { get; set; }
    public RefreshToken? ReplacedByToken { get; set; }

    /// <summary>"Remember me": a long-lived session. Carried over to the replacement token.</summary>
    public bool IsPersistent { get; set; }
}
