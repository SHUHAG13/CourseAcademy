using CourseAcademy.Api.Common.Auth;
using CourseAcademy.Api.Entities;

namespace CourseAcademy.Api.Data.Seed;

/// <summary>
/// Screens, permissions and built-in roles, inserted by migrations. They mirror the frontend's
/// <c>screens.data.ts</c> and <c>permission.model.ts</c>. Ids are fixed; never renumber an existing row.
/// </summary>
public static class AccessSeed
{
    private static readonly DateTime SeededAt = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

    public const int AdminRoleId = 1;
    public const int InstructorRoleId = 2;
    public const int StudentRoleId = 3;

    private static Screen NewScreen(int id, string code, string name, string area, string path, string icon,
        bool courseScoped = false, string? section = null) => new()
    {
        Id = id, Code = code, Name = name, SortOrder = id, Area = area, Path = path, Icon = icon,
        IsCourseScoped = courseScoped, Section = section, CreatedAt = SeededAt,
    };

    public static readonly Screen[] Screens =
    [
        NewScreen(1, "overview", "Overview", Screen.AdminArea, "/admin", "bi-speedometer2"),
        NewScreen(2, "courses", "Courses", Screen.AdminArea, "/admin/courses", "bi-journal-text", courseScoped: true),
        NewScreen(3, "instructors", "Instructors", Screen.AdminArea, "/admin/instructors", "bi-person-video3"),
        NewScreen(4, "users", "Users", Screen.AdminArea, "/admin/users", "bi-people"),
        NewScreen(5, "enrollments", "Enrollments", Screen.AdminArea, "/admin/enrollments", "bi-card-checklist"),
        NewScreen(6, "transactions", "Transactions", Screen.AdminArea, "/admin/transactions", "bi-credit-card"),
        NewScreen(7, "roles", "Roles & permissions", Screen.AdminArea, "/admin/security/roles", "bi-person-badge", section: "security"),
        NewScreen(8, "access", "User access", Screen.AdminArea, "/admin/security/access", "bi-person-lock", section: "security"),
        NewScreen(9, "community-moderation", "Community", Screen.AdminArea, "/admin/community", "bi-chat-square-text", courseScoped: true),
        NewScreen(10, "certificates", "Certificates", Screen.AdminArea, "/admin/certificates", "bi-award", courseScoped: true),
        NewScreen(11, "dashboard", "My dashboard", Screen.StudentArea, "/dashboard", "bi-grid"),
        NewScreen(12, "learn", "Learning area", Screen.StudentArea, "/dashboard", "bi-play-circle"),
        NewScreen(13, "checkout", "Checkout", Screen.StudentArea, "/courses", "bi-bag"),
        NewScreen(14, "community", "Course community", Screen.StudentArea, "/dashboard", "bi-chat-dots"),
    ];

    private static Permission NewPermission(int id, int screenId, string code, string name, string action, bool grantable = false) => new()
    {
        Id = id, ScreenId = screenId, Code = code, Name = name, Action = action, SortOrder = id,
        IsCourseGrantable = grantable, CreatedAt = SeededAt,
    };

    public static readonly Permission[] Permissions =
    [
        NewPermission(1, 1, PermissionCodes.ReportsView, "View the overview and reports", "view"),
        NewPermission(2, 1, PermissionCodes.SystemReset, "Reset demo data", "extra"),
        NewPermission(3, 2, PermissionCodes.CoursesView, "View courses, including drafts", "view", grantable: true),
        NewPermission(4, 2, PermissionCodes.CoursesCreate, "Create courses", "create"),
        NewPermission(5, 2, PermissionCodes.CoursesEdit, "Edit and publish courses", "edit", grantable: true),
        NewPermission(6, 2, PermissionCodes.CoursesDelete, "Delete courses", "delete"),
        NewPermission(7, 3, PermissionCodes.InstructorsView, "View instructors", "view"),
        NewPermission(8, 3, PermissionCodes.InstructorsCreate, "Create instructors", "create"),
        NewPermission(9, 3, PermissionCodes.InstructorsEdit, "Edit instructors", "edit"),
        NewPermission(10, 3, PermissionCodes.InstructorsDelete, "Delete instructors", "delete"),
        NewPermission(11, 4, PermissionCodes.UsersView, "View users", "view"),
        NewPermission(12, 5, PermissionCodes.EnrollmentsView, "View enrollments", "view"),
        NewPermission(13, 5, PermissionCodes.EnrollmentsEdit, "Change enrollments", "edit"),
        NewPermission(14, 6, PermissionCodes.TransactionsView, "View transactions", "view"),
        NewPermission(15, 6, PermissionCodes.TransactionsEdit, "Change transactions", "edit"),
        NewPermission(16, 6, PermissionCodes.TransactionsDelete, "Delete transactions", "delete"),
        NewPermission(17, 7, PermissionCodes.RolesView, "View roles", "view"),
        NewPermission(18, 7, PermissionCodes.RolesEdit, "Create, edit and assign roles", "edit"),
        NewPermission(19, 8, PermissionCodes.AccessView, "View user access", "view"),
        NewPermission(20, 8, PermissionCodes.AccessEdit, "Change user access", "edit"),
        NewPermission(21, 9, PermissionCodes.CommunityModerate, "Moderate course communities", "view", grantable: true),
        NewPermission(22, 10, PermissionCodes.CertificatesManage, "Set certification rules, review exams and manage certificates", "view", grantable: true),
        NewPermission(23, 11, PermissionCodes.DashboardView, "Open the student dashboard", "view"),
        NewPermission(24, 12, PermissionCodes.LearnView, "Open enrolled courses", "view"),
        NewPermission(25, 13, PermissionCodes.CheckoutView, "Buy courses", "view"),
        NewPermission(26, 14, PermissionCodes.CommunityView, "Read course communities", "view"),
        NewPermission(27, 14, PermissionCodes.CommunityPost, "Write posts, comments and replies", "create"),
    ];

    public static readonly Role[] Roles =
    [
        new() { Id = AdminRoleId, Code = Role.AdminCode, Name = "Admin", Description = "Full access to every part of the academy.", IsSystem = true, CreatedAt = SeededAt },
        new() { Id = InstructorRoleId, Code = Role.InstructorCode, Name = "Instructor", Description = "Opens the admin panel for the courses they are granted.", IsSystem = true, CreatedAt = SeededAt },
        new() { Id = StudentRoleId, Code = Role.StudentCode, Name = "Student", Description = "Learns in the courses they are enrolled in.", IsSystem = true, CreatedAt = SeededAt },
    ];

    /// <summary>Instructors and students start with the four student screens. The admin role needs no rows.</summary>
    public static readonly RoleScreen[] RoleScreens =
    [
        .. new[] { InstructorRoleId, StudentRoleId }.SelectMany((roleId, roleIndex) =>
            new[] { 11, 12, 13, 14 }.Select((screenId, screenIndex) => new RoleScreen
            {
                Id = roleIndex * 4 + screenIndex + 1, RoleId = roleId, ScreenId = screenId, CreatedAt = SeededAt,
            })),
    ];
}
