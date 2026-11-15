namespace CourseAcademy.Api.Common.Auth;

/// <summary>Permission codes, identical to the frontend's <c>Permission</c> type and the Permission table's Code column.</summary>
public static class PermissionCodes
{
    public const string ReportsView = "reports.view";
    public const string SystemReset = "system.reset";
    public const string CoursesView = "courses.view";
    public const string CoursesCreate = "courses.create";
    public const string CoursesEdit = "courses.edit";
    public const string CoursesDelete = "courses.delete";
    public const string InstructorsView = "instructors.view";
    public const string InstructorsCreate = "instructors.create";
    public const string InstructorsEdit = "instructors.edit";
    public const string InstructorsDelete = "instructors.delete";
    public const string UsersView = "users.view";
    public const string EnrollmentsView = "enrollments.view";
    public const string EnrollmentsEdit = "enrollments.edit";
    public const string TransactionsView = "transactions.view";
    public const string TransactionsEdit = "transactions.edit";
    public const string TransactionsDelete = "transactions.delete";
    public const string RolesView = "roles.view";
    public const string RolesEdit = "roles.edit";
    public const string AccessView = "access.view";
    public const string AccessEdit = "access.edit";
    public const string DashboardView = "dashboard.view";
    public const string LearnView = "learn.view";
    public const string CheckoutView = "checkout.view";
    public const string CommunityView = "community.view";
    public const string CommunityPost = "community.post";
    public const string CommunityModerate = "community.moderate";
    public const string CertificatesManage = "certificates.manage";
}
