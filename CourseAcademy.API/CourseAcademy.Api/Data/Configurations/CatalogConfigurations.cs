using CourseAcademy.Api.Data.Seed;
using CourseAcademy.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseAcademy.Api.Data.Configurations;

public sealed class CategorySkillConfiguration : IEntityTypeConfiguration<CategorySkill>
{
    public void Configure(EntityTypeBuilder<CategorySkill> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.Skill).HasMaxLength(100).IsRequired();
        builder.HasOne(e => e.Category).WithMany(c => c.Skills).HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(e => new { e.CategoryId, e.SortOrder });
        builder.HasData(LookupSeed.CategorySkills);
    }
}

public sealed class InstructorConfiguration : IEntityTypeConfiguration<Instructor>
{
    public void Configure(EntityTypeBuilder<Instructor> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Bio).HasMaxLength(4000).IsRequired();
        builder.Property(e => e.Rating).HasPrecision(2, 1);
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Instructor_ExperienceYears", "[ExperienceYears] >= 0");
            t.HasCheckConstraint("CK_Instructor_Students", "[Students] >= 0");
            t.HasCheckConstraint("CK_Instructor_Rating", "[Rating] BETWEEN 0 AND 5");
        });
    }
}

public sealed class InstructorExpertiseConfiguration : IEntityTypeConfiguration<InstructorExpertise>
{
    public void Configure(EntityTypeBuilder<InstructorExpertise> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.Expertise).HasMaxLength(100).IsRequired();
        builder.HasOne(e => e.Instructor).WithMany(i => i.Expertise).HasForeignKey(e => e.InstructorId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(e => new { e.InstructorId, e.SortOrder });
    }
}

public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.Slug).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(120).IsRequired();
        builder.Property(e => e.Subtitle).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(8000).IsRequired();
        builder.Property(e => e.Language).HasMaxLength(50).IsRequired();
        builder.Property(e => e.CoverLabel).HasMaxLength(30).IsRequired();
        builder.Property(e => e.CoverIcon).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ImageUrl).HasMaxLength(2048);
        builder.Property(e => e.Rating).HasPrecision(2, 1);
        builder.Property(e => e.ContentUpdatedMonth).HasColumnType("char(7)").IsRequired();

        builder.HasIndex(e => e.Slug).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(e => new { e.IsPublished, e.CategoryId });
        builder.HasIndex(e => e.InstructorId);
        builder.HasIndex(e => e.IsFeatured);

        builder.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.Format).WithMany().HasForeignKey(e => e.CourseFormatId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.Level).WithMany().HasForeignKey(e => e.CourseLevelId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.Badge).WithMany().HasForeignKey(e => e.CourseBadgeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.Instructor).WithMany().HasForeignKey(e => e.InstructorId).OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(e => e.Outcomes).WithOne().HasForeignKey(o => o.CourseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasMany(e => e.Requirements).WithOne().HasForeignKey(r => r.CourseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasMany(e => e.Modules).WithOne().HasForeignKey(m => m.CourseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasMany(e => e.Sessions).WithOne().HasForeignKey(s => s.CourseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasMany(e => e.Materials).WithOne().HasForeignKey(m => m.CourseId).OnDelete(DeleteBehavior.NoAction);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Course_Prices", "[Price] >= 0 AND [OriginalPrice] >= 0 AND [Price] <= [OriginalPrice]");
            t.HasCheckConstraint("CK_Course_DurationHours", "[DurationHours] >= 1");
            t.HasCheckConstraint("CK_Course_Rating", "[Rating] BETWEEN 0 AND 5");
            t.HasCheckConstraint("CK_Course_Counts", "[RatingCount] >= 0 AND [Students] >= 0");
        });
    }
}

public sealed class CourseOutcomeConfiguration : IEntityTypeConfiguration<CourseOutcome>
{
    public void Configure(EntityTypeBuilder<CourseOutcome> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.Text).HasMaxLength(500).IsRequired();
        builder.HasIndex(e => new { e.CourseId, e.SortOrder });
    }
}

public sealed class CourseRequirementConfiguration : IEntityTypeConfiguration<CourseRequirement>
{
    public void Configure(EntityTypeBuilder<CourseRequirement> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.Text).HasMaxLength(500).IsRequired();
        builder.HasIndex(e => new { e.CourseId, e.SortOrder });
    }
}

public sealed class CourseModuleConfiguration : IEntityTypeConfiguration<CourseModule>
{
    public void Configure(EntityTypeBuilder<CourseModule> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.HasMany(e => e.Submodules).WithOne().HasForeignKey(s => s.CourseModuleId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(e => new { e.CourseId, e.SortOrder });
    }
}

public sealed class SubmoduleConfiguration : IEntityTypeConfiguration<Submodule>
{
    public void Configure(EntityTypeBuilder<Submodule> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.HasMany(e => e.Sections).WithOne().HasForeignKey(s => s.SubmoduleId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(e => new { e.CourseModuleId, e.SortOrder });
    }
}

public sealed class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public void Configure(EntityTypeBuilder<Section> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.HasOne(e => e.ParentSection).WithMany(s => s.Topics).HasForeignKey(e => e.ParentSectionId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Course>().WithMany().HasForeignKey(e => e.CourseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasMany(e => e.Videos).WithOne().HasForeignKey(v => v.SectionId).OnDelete(DeleteBehavior.NoAction);
        builder.HasMany(e => e.Documents).WithOne().HasForeignKey(d => d.SectionId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(e => new { e.SubmoduleId, e.SortOrder });
        builder.HasIndex(e => new { e.ParentSectionId, e.SortOrder });
        builder.HasIndex(e => e.CourseId);
        builder.ToTable(t =>
        {
            // A section hangs under a submodule (depth 0) or under another section (a topic, depth 1–3).
            t.HasCheckConstraint("CK_Section_Parent",
                "([SubmoduleId] IS NOT NULL AND [ParentSectionId] IS NULL AND [Depth] = 0) OR " +
                "([SubmoduleId] IS NULL AND [ParentSectionId] IS NOT NULL AND [Depth] BETWEEN 1 AND 3)");
        });
    }
}

public sealed class SectionVideoConfiguration : IEntityTypeConfiguration<SectionVideo>
{
    public void Configure(EntityTypeBuilder<SectionVideo> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ExternalUrl).HasMaxLength(2048);
        builder.Property(e => e.AssetId).HasMaxLength(100);
        builder.HasIndex(e => new { e.SectionId, e.SortOrder });
        builder.HasIndex(e => e.AssetId).HasFilter("[AssetId] IS NOT NULL");
        builder.ToTable(t => t.HasCheckConstraint("CK_SectionVideo_Minutes", "[Minutes] >= 1"));
    }
}

public sealed class SectionDocumentConfiguration : IEntityTypeConfiguration<SectionDocument>
{
    public void Configure(EntityTypeBuilder<SectionDocument> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ExternalUrl).HasMaxLength(2048);
        builder.Property(e => e.AssetId).HasMaxLength(100);
        builder.HasOne(e => e.DocumentType).WithMany().HasForeignKey(e => e.DocumentTypeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.Video).WithMany().HasForeignKey(e => e.SectionVideoId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(e => new { e.SectionId, e.SortOrder });
        builder.HasIndex(e => e.AssetId).HasFilter("[AssetId] IS NOT NULL");
    }
}

public sealed class LiveSessionConfiguration : IEntityTypeConfiguration<LiveSession>
{
    public void Configure(EntityTypeBuilder<LiveSession> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.MeetingUrl).HasMaxLength(2048).IsRequired();
        builder.HasIndex(e => new { e.CourseId, e.StartsAtUtc });
        builder.ToTable(t => t.HasCheckConstraint("CK_LiveSession_Duration", "[DurationMinutes] >= 1"));
    }
}

public sealed class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ExternalUrl).HasMaxLength(2048);
        builder.Property(e => e.AssetId).HasMaxLength(100);
        builder.HasOne(e => e.MaterialType).WithMany().HasForeignKey(e => e.MaterialTypeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(e => new { e.CourseId, e.SortOrder });
        builder.HasIndex(e => e.AssetId).HasFilter("[AssetId] IS NOT NULL");
    }
}
