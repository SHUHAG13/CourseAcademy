using CourseAcademy.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseAcademy.Api.Data.Configurations;

public sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.Title).HasMaxLength(120).IsRequired();
        builder.Property(e => e.Body).HasMaxLength(2000).IsRequired();
        builder.Property(e => e.IsPinned).HasDefaultValue(false);
        builder.Property(e => e.IsLocked).HasDefaultValue(false);
        builder.Property(e => e.IsHidden).HasDefaultValue(false);
        // Pinned first, then newest (BR-COM-05).
        builder.HasIndex(e => new { e.CourseId, e.IsPinned, e.CreatedAt }).IsDescending(false, true, true);
        builder.HasIndex(e => e.AuthorUserId);

        builder.HasOne<Course>().WithMany().HasForeignKey(e => e.CourseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.AuthorUserId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.Body).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.IsHidden).HasDefaultValue(false);
        builder.HasIndex(e => new { e.PostId, e.CreatedAt });
        builder.HasIndex(e => e.ParentCommentId);
        builder.HasIndex(e => new { e.CourseId, e.CreatedAt });

        builder.HasOne<Post>().WithMany().HasForeignKey(e => e.PostId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Course>().WithMany().HasForeignKey(e => e.CourseId).OnDelete(DeleteBehavior.NoAction);
        // Replies point at a top-level comment only (BR-COM-07, checked by the service).
        builder.HasOne<Comment>().WithMany().HasForeignKey(e => e.ParentCommentId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.AuthorUserId).OnDelete(DeleteBehavior.NoAction);
    }
}
