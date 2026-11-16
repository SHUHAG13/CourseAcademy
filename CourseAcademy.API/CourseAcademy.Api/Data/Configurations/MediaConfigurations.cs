using CourseAcademy.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseAcademy.Api.Data.Configurations;

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.AssetId).HasMaxLength(100);
        builder.Property(e => e.FileName).HasMaxLength(255).IsRequired();
        builder.Property(e => e.MimeType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ErrorCode).HasMaxLength(100);
        builder.Property(e => e.ErrorMessage).HasMaxLength(500);
        // Two webhooks for the same asset at once: only the first change is saved; Streaming retries the other.
        builder.Property(e => e.LastSequence).IsConcurrencyToken();

        builder.HasIndex(e => e.AssetId).IsUnique().HasFilter("[AssetId] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(e => e.CourseId);
        builder.HasIndex(e => new { e.MediaAssetStatusId, e.DeleteRequestedAt });

        builder.HasOne(e => e.Kind).WithMany().HasForeignKey(e => e.MediaKindId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.Status).WithMany().HasForeignKey(e => e.MediaAssetStatusId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Course>().WithMany().HasForeignKey(e => e.CourseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.UploadedByUserId).OnDelete(DeleteBehavior.NoAction);

        builder.ToTable(t => t.HasCheckConstraint("CK_MediaAsset_Numbers",
            "[SizeBytes] > 0 AND ([DurationSeconds] IS NULL OR [DurationSeconds] >= 0) AND [LastSequence] >= 0"));
    }
}

public sealed class StreamingWebhookEventConfiguration : IEntityTypeConfiguration<StreamingWebhookEvent>
{
    public void Configure(EntityTypeBuilder<StreamingWebhookEvent> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.EventId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(50).IsRequired();
        builder.Property(e => e.AssetId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Outcome).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Payload).HasColumnType("nvarchar(max)").IsRequired();
        // Not filtered: a repeated event is a repeat even if the first copy was ever soft-deleted.
        builder.HasIndex(e => e.EventId).IsUnique();
        builder.HasIndex(e => e.AssetId);
    }
}
