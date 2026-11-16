using CourseAcademy.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseAcademy.Api.Data.Configurations;

public sealed class LessonProgressConfiguration : IEntityTypeConfiguration<LessonProgress>
{
    public void Configure(EntityTypeBuilder<LessonProgress> builder)
    {
        builder.ConfigureBase();
        // SQL Server treats NULLs as equal in a unique index, so a section without videos also has one row per user.
        builder.HasIndex(e => new { e.UserId, e.SectionId, e.SectionVideoId }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(e => new { e.UserId, e.CourseId });
        builder.HasIndex(e => e.SectionVideoId);

        builder.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Course>().WithMany().HasForeignKey(e => e.CourseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Section>().WithMany().HasForeignKey(e => e.SectionId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<SectionVideo>().WithMany().HasForeignKey(e => e.SectionVideoId).OnDelete(DeleteBehavior.NoAction);

        builder.ToTable(t => t.HasCheckConstraint("CK_LessonProgress_Seconds",
            "[PositionSeconds] >= 0 AND [MaxWatchedSeconds] >= 0 AND ([DurationSeconds] IS NULL OR [DurationSeconds] > 0)"));
    }
}
