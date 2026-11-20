namespace CourseAcademy.Api.Entities;

/// <summary>A discussion started in one course's community.</summary>
public sealed class Post : BaseEntity
{
    public int CourseId { get; set; }
    public int AuthorUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime? EditedAt { get; set; }
    /// <summary>Shown above the other posts.</summary>
    public bool IsPinned { get; set; }
    /// <summary>No new comments, except from moderators.</summary>
    public bool IsLocked { get; set; }
    /// <summary>Removed from view by a moderator. Moderators still see it.</summary>
    public bool IsHidden { get; set; }
}

/// <summary>A comment on a post or, with <see cref="ParentCommentId"/>, a reply to a top-level comment (one level deep).</summary>
public sealed class Comment : BaseEntity
{
    public int PostId { get; set; }
    /// <summary>Copied from the post, for per-course queries.</summary>
    public int CourseId { get; set; }
    public int? ParentCommentId { get; set; }
    public int AuthorUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime? EditedAt { get; set; }
    public bool IsHidden { get; set; }
}
