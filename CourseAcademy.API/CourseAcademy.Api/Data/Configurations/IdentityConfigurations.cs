using CourseAcademy.Api.Data.Seed;
using CourseAcademy.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseAcademy.Api.Data.Configurations;

public static class EntityConfigurationExtensions
{
    /// <summary>Table name, key and the defaults of the standard columns.</summary>
    public static EntityTypeBuilder<T> ConfigureBase<T>(this EntityTypeBuilder<T> builder) where T : BaseEntity
    {
        builder.ToTable(typeof(T).Name);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("sysutcdatetime()");
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        return builder;
    }
}

public sealed class ScreenConfiguration : LookupConfiguration<Screen>
{
    protected override IEnumerable<Screen> Seed => AccessSeed.Screens;

    public override void Configure(EntityTypeBuilder<Screen> builder)
    {
        base.Configure(builder);
        builder.Property(e => e.Area).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Path).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Icon).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Section).HasMaxLength(50);
        builder.ToTable(t => t.HasCheckConstraint("CK_Screen_Area", "[Area] IN ('admin', 'student')"));
    }
}

public sealed class PermissionConfiguration : LookupConfiguration<Permission>
{
    protected override IEnumerable<Permission> Seed => AccessSeed.Permissions;

    public override void Configure(EntityTypeBuilder<Permission> builder)
    {
        base.Configure(builder);
        builder.Property(e => e.Action).HasMaxLength(20).IsRequired();
        builder.HasOne(e => e.Screen).WithMany(s => s.Permissions).HasForeignKey(e => e.ScreenId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.HasIndex(e => e.Code).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(e => e.Name).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasData(AccessSeed.Roles);
    }
}

public sealed class RoleScreenConfiguration : IEntityTypeConfiguration<RoleScreen>
{
    public void Configure(EntityTypeBuilder<RoleScreen> builder)
    {
        builder.ConfigureBase();
        builder.HasIndex(e => new { e.RoleId, e.ScreenId }).IsUnique();
        builder.HasOne(e => e.Role).WithMany(r => r.Screens).HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.Screen).WithMany().HasForeignKey(e => e.ScreenId).OnDelete(DeleteBehavior.NoAction);
        builder.HasData(AccessSeed.RoleScreens);
    }
}

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Phone).HasMaxLength(20);
        builder.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(e => e.HasCustomAccess).HasDefaultValue(false);
        builder.HasIndex(e => e.Email).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(e => e.RoleId);
        builder.HasOne(e => e.Role).WithMany().HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.Instructor).WithMany().HasForeignKey(e => e.InstructorId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(e => e.InstructorId).IsUnique().HasFilter("[InstructorId] IS NOT NULL AND [IsDeleted] = 0");
    }
}

public sealed class UserScreenConfiguration : IEntityTypeConfiguration<UserScreen>
{
    public void Configure(EntityTypeBuilder<UserScreen> builder)
    {
        builder.ConfigureBase();
        builder.HasIndex(e => new { e.UserId, e.ScreenId }).IsUnique();
        builder.HasOne(e => e.User).WithMany(u => u.Screens).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.Screen).WithMany().HasForeignKey(e => e.ScreenId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class CourseGrantConfiguration : IEntityTypeConfiguration<CourseGrant>
{
    public void Configure(EntityTypeBuilder<CourseGrant> builder)
    {
        builder.ConfigureBase();
        builder.HasIndex(e => new { e.UserId, e.CourseId }).IsUnique();
        builder.HasIndex(e => e.CourseId);
        builder.HasOne(e => e.User).WithMany(u => u.CourseGrants).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.Course).WithMany().HasForeignKey(e => e.CourseId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class CourseGrantPermissionConfiguration : IEntityTypeConfiguration<CourseGrantPermission>
{
    public void Configure(EntityTypeBuilder<CourseGrantPermission> builder)
    {
        builder.ConfigureBase();
        builder.HasIndex(e => new { e.CourseGrantId, e.PermissionId }).IsUnique();
        builder.HasOne(e => e.CourseGrant).WithMany(g => g.Permissions).HasForeignKey(e => e.CourseGrantId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.Permission).WithMany().HasForeignKey(e => e.PermissionId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.TokenHash).HasMaxLength(64).IsRequired();
        // Two requests using the same token at once: only the first save wins.
        builder.Property(e => e.RevokedAt).IsConcurrencyToken();
        builder.HasIndex(e => e.TokenHash).IsUnique();
        builder.HasIndex(e => new { e.UserId, e.ExpiresAt });
        builder.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.ReplacedByToken).WithMany().HasForeignKey(e => e.ReplacedByTokenId).OnDelete(DeleteBehavior.NoAction);
    }
}
