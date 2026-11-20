using CourseAcademy.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseAcademy.Api.Data.Configurations;

public sealed class CertificationRuleConfiguration : IEntityTypeConfiguration<CertificationRule>
{
    public void Configure(EntityTypeBuilder<CertificationRule> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.IsEnabled).HasDefaultValue(true);
        builder.Property(e => e.MinProgress).HasDefaultValue(100);
        builder.Property(e => e.ExamTitle).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ExamInstructions).HasColumnType("nvarchar(max)");
        builder.Property(e => e.MaxScore).HasDefaultValue(100);
        builder.Property(e => e.PassingScore).HasDefaultValue(60);
        builder.Property(e => e.MaxAttempts).HasDefaultValue(0);
        builder.HasIndex(e => e.CourseId).IsUnique().HasFilter("[IsDeleted] = 0");

        builder.HasOne<Course>().WithMany().HasForeignKey(e => e.CourseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<CertificationMode>().WithMany().HasForeignKey(e => e.CertificationModeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<ExamType>().WithMany().HasForeignKey(e => e.ExamTypeId).OnDelete(DeleteBehavior.NoAction);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_CertificationRule_MinProgress", "[MinProgress] BETWEEN 1 AND 100");
            t.HasCheckConstraint("CK_CertificationRule_Scores", "[MaxScore] >= 1 AND [PassingScore] BETWEEN 1 AND [MaxScore]");
            t.HasCheckConstraint("CK_CertificationRule_MaxAttempts", "[MaxAttempts] >= 0");
        });
    }
}

public sealed class ExamSubmissionConfiguration : IEntityTypeConfiguration<ExamSubmission>
{
    public void Configure(EntityTypeBuilder<ExamSubmission> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.Url).HasMaxLength(2048).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.Property(e => e.Score).HasPrecision(9, 2);
        builder.Property(e => e.Feedback).HasMaxLength(1000);
        // Two reviewers saving the same hand-in at once: only the first review is saved.
        builder.Property(e => e.SubmissionStatusId).IsConcurrencyToken();
        builder.HasIndex(e => new { e.CourseId, e.SubmissionStatusId, e.SubmittedAt }).IsDescending(false, false, true);
        builder.HasIndex(e => new { e.UserId, e.CourseId });

        builder.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Course>().WithMany().HasForeignKey(e => e.CourseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<ExamType>().WithMany().HasForeignKey(e => e.ExamTypeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<SubmissionStatus>().WithMany().HasForeignKey(e => e.SubmissionStatusId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.ReviewedByUserId).OnDelete(DeleteBehavior.NoAction);

        builder.ToTable(t => t.HasCheckConstraint("CK_ExamSubmission_Score", "[Score] IS NULL OR [Score] >= 0"));
    }
}

public sealed class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.Code).HasMaxLength(14).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(e => e.UserName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.CourseTitle).HasMaxLength(120).IsRequired();
        builder.Property(e => e.InstructorName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.IsRevoked).HasDefaultValue(false);
        builder.Property(e => e.RevokedReason).HasMaxLength(200);
        builder.HasIndex(e => e.Code).IsUnique().HasFilter("[IsDeleted] = 0");
        // At most one valid certificate per user and course (BR-CERT-03).
        builder.HasIndex(e => new { e.UserId, e.CourseId }).IsUnique().HasFilter("[IsRevoked] = 0 AND [IsDeleted] = 0");
        builder.HasIndex(e => e.CourseId);

        builder.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.NoAction);
        // The course row is only ever soft-deleted, so the reference stays valid and the certificate still verifies.
        builder.HasOne<Course>().WithMany().HasForeignKey(e => e.CourseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<CertificationMode>().WithMany().HasForeignKey(e => e.CertificationModeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<ExamType>().WithMany().HasForeignKey(e => e.ExamTypeId).OnDelete(DeleteBehavior.NoAction);

        builder.ToTable(t => t.HasCheckConstraint("CK_Certificate_ScorePercent", "[ScorePercent] IS NULL OR [ScorePercent] BETWEEN 0 AND 100"));
    }
}
