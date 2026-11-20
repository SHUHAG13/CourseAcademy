using CourseAcademy.Api.Entities;

namespace CourseAcademy.Api.Data;

/// <summary>
/// SQL Server schema of every table, grouped by area (e.g. <c>Securities.User</c>, <c>Catalog.Section</c>).
/// Every entity must be listed: the model fails to build otherwise, so a new table never lands in <c>dbo</c> by accident.
/// </summary>
public static class DbSchemas
{
    public const string Securities = "Securities";
    public const string Lookups = "Lookups";
    public const string Catalog = "Catalog";
    public const string Commerce = "Commerce";
    public const string Learning = "Learning";
    public const string Media = "Media";
    public const string Certification = "Certification";
    public const string Community = "Community";

    private static readonly Dictionary<Type, string> Map = new()
    {
        // Users, roles, screen access, course grants and sign-in tokens.
        [typeof(User)] = Securities,
        [typeof(Role)] = Securities,
        [typeof(Screen)] = Securities,
        [typeof(Permission)] = Securities,
        [typeof(RoleScreen)] = Securities,
        [typeof(UserScreen)] = Securities,
        [typeof(CourseGrant)] = Securities,
        [typeof(CourseGrantPermission)] = Securities,
        [typeof(RefreshToken)] = Securities,

        // Fixed lists of values.
        [typeof(CourseFormat)] = Lookups,
        [typeof(CourseLevel)] = Lookups,
        [typeof(CourseBadge)] = Lookups,
        [typeof(Category)] = Lookups,
        [typeof(CategorySkill)] = Lookups,
        [typeof(DocumentType)] = Lookups,
        [typeof(MaterialType)] = Lookups,
        [typeof(EnrollmentStatus)] = Lookups,
        [typeof(PaymentStatus)] = Lookups,
        [typeof(PaymentMethod)] = Lookups,
        [typeof(Currency)] = Lookups,
        [typeof(MediaKind)] = Lookups,
        [typeof(MediaAssetStatus)] = Lookups,
        [typeof(CertificationMode)] = Lookups,
        [typeof(ExamType)] = Lookups,
        [typeof(SubmissionStatus)] = Lookups,

        // Courses, their curriculum and instructors.
        [typeof(Instructor)] = Catalog,
        [typeof(InstructorExpertise)] = Catalog,
        [typeof(Course)] = Catalog,
        [typeof(CourseOutcome)] = Catalog,
        [typeof(CourseRequirement)] = Catalog,
        [typeof(CourseModule)] = Catalog,
        [typeof(Submodule)] = Catalog,
        [typeof(Section)] = Catalog,
        [typeof(SectionVideo)] = Catalog,
        [typeof(SectionDocument)] = Catalog,
        [typeof(LiveSession)] = Catalog,
        [typeof(Material)] = Catalog,

        [typeof(Enrollment)] = Commerce,
        [typeof(PaymentTransaction)] = Commerce,

        [typeof(LessonProgress)] = Learning,

        [typeof(MediaAsset)] = Media,
        [typeof(StreamingWebhookEvent)] = Media,

        // Certificate rules, central exam hand-ins and issued certificates.
        [typeof(CertificationRule)] = Certification,
        [typeof(ExamSubmission)] = Certification,
        [typeof(Certificate)] = Certification,

        // Course discussions.
        [typeof(Post)] = Community,
        [typeof(Comment)] = Community,
    };

    public static string Of(Type entityType) =>
        Map.TryGetValue(entityType, out var schema)
            ? schema
            : throw new InvalidOperationException($"No database schema is set for {entityType.Name}. Add it to {nameof(DbSchemas)}.");
}
