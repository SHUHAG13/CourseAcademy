namespace CourseAcademy.Api.Dtos;

/// <summary>What the signed-in user (or a visitor) may do in a course's community, and its visible post count.</summary>
public sealed record CommunityInfoDto(bool CanRead, bool CanPost, bool CanModerate, int PublicPostCount);

/// <summary>Id is null and Name "Former member" when the account was deleted (BR-COM-13). Badge: admin, instructor or student.</summary>
public sealed record AuthorDto(int? Id, string Name, string Badge);

public sealed record PostDto(
    int Id, int CourseId, string Title, string Body, AuthorDto Author, DateTime CreatedAt, DateTime? EditedAt,
    bool IsPinned, bool IsLocked, bool IsHidden, int CommentCount, bool CanEdit, bool CanDelete, bool CanComment);

/// <summary>A top-level comment with its replies, or a reply (empty Replies).</summary>
public sealed record CommentDto(
    int Id, int PostId, int CourseId, int? ParentId, string Body, AuthorDto Author, DateTime CreatedAt, DateTime? EditedAt,
    bool IsHidden, bool CanEdit, bool CanDelete, IReadOnlyList<CommentDto> Replies);

public sealed record PostSaveRequest(string? Title, string? Body);

public sealed record CommentAddRequest(string? Body, int? ParentId);

public sealed record CommentEditRequest(string? Body);

/// <summary>Only the flags that are sent change.</summary>
public sealed record PostFlagsRequest(bool? IsPinned, bool? IsLocked, bool? IsHidden);

public sealed record CommentHiddenRequest(bool IsHidden);

// ── Moderation lists ────────────────────────────────────────────

public sealed record ModerationPostDto(PostDto Post, string CourseTitle, string CourseSlug);

public sealed record ModerationCommentDto(CommentDto Comment, string PostTitle, string CourseTitle, string CourseSlug);

public sealed class ModerationPostQuery : Common.PageQuery
{
    public int? CourseId { get; set; }
    /// <summary>all (default), visible, hidden, pinned or locked.</summary>
    public string? Status { get; set; }
    /// <summary>Matches the title, the text or the author's name.</summary>
    public string? Search { get; set; }
}

public sealed class ModerationCommentQuery
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 100;

    private int _limit = DefaultLimit;

    public int? CourseId { get; set; }
    /// <summary>all (default), visible or hidden.</summary>
    public string? Status { get; set; }
    /// <summary>Matches the text or the author's name.</summary>
    public string? Search { get; set; }

    /// <summary>Newest comments first; out-of-range values are clamped.</summary>
    public int Limit
    {
        get => _limit;
        set => _limit = value < 1 ? DefaultLimit : Math.Min(value, MaxLimit);
    }
}
