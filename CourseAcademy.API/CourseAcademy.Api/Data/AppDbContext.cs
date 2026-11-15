using System.Linq.Expressions;
using CourseAcademy.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CourseAcademy.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<CourseFormat> CourseFormats => Set<CourseFormat>();
    public DbSet<CourseLevel> CourseLevels => Set<CourseLevel>();
    public DbSet<CourseBadge> CourseBadges => Set<CourseBadge>();
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<MaterialType> MaterialTypes => Set<MaterialType>();
    public DbSet<EnrollmentStatus> EnrollmentStatuses => Set<EnrollmentStatus>();
    public DbSet<PaymentStatus> PaymentStatuses => Set<PaymentStatus>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();

    public DbSet<Screen> Screens => Set<Screen>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RoleScreen> RoleScreens => Set<RoleScreen>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserScreen> UserScreens => Set<UserScreen>();
    public DbSet<CourseGrant> CourseGrants => Set<CourseGrant>();
    public DbSet<CourseGrantPermission> CourseGrantPermissions => Set<CourseGrantPermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<CategorySkill> CategorySkills => Set<CategorySkill>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<InstructorExpertise> InstructorExpertise => Set<InstructorExpertise>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseOutcome> CourseOutcomes => Set<CourseOutcome>();
    public DbSet<CourseRequirement> CourseRequirements => Set<CourseRequirement>();
    public DbSet<CourseModule> CourseModules => Set<CourseModule>();
    public DbSet<Submodule> Submodules => Set<Submodule>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<SectionVideo> SectionVideos => Set<SectionVideo>();
    public DbSet<SectionDocument> SectionDocuments => Set<SectionDocument>();
    public DbSet<LiveSession> LiveSessions => Set<LiveSession>();
    public DbSet<Material> Materials => Set<Material>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ApplySoftDeleteFilters(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // All times are stored as UTC. SQL Server returns them without a kind, so mark them UTC on read
        // so they serialize with a trailing "Z".
        configurationBuilder.Properties<DateTime>().HaveColumnType("datetime2(3)").HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        configurationBuilder.Properties<string>().HaveMaxLength(200);
    }

    /// <summary>Hides soft-deleted rows from every query. Use IgnoreQueryFilters() to see them.</summary>
    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType) || entityType.BaseType is not null) continue;
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var body = Expression.Not(Expression.Property(parameter, nameof(BaseEntity.IsDeleted)));
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(body, parameter));
        }
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
}
