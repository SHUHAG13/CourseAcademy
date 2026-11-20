using CourseAcademy.Api.Common;
using CourseAcademy.Api.Common.Auth;
using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Api.Services;

/// <summary>Course discussion boards and their moderation (BR-COM-01 to 16).</summary>
public interface ICommunityService
{
    /// <summary>What the caller may do in a course's community. Works for visitors too.</summary>
    Task<CommunityInfoDto> GetInfoAsync(string courseSlug, CancellationToken ct);

    Task<PagedResult<PostDto>> GetPostsAsync(string courseSlug, PageQuery page, CancellationToken ct);
    Task<PostDto> CreatePostAsync(string courseSlug, PostSaveRequest request, CancellationToken ct);
    Task<PostDto> UpdatePostAsync(int postId, PostSaveRequest request, CancellationToken ct);
    Task DeletePostAsync(int postId, CancellationToken ct);
    Task<PostDto> SetPostFlagsAsync(int postId, PostFlagsRequest request, CancellationToken ct);

    /// <summary>Top-level comments oldest first, each with its replies.</summary>
    Task<IReadOnlyList<CommentDto>> GetCommentsAsync(int postId, CancellationToken ct);
    Task<CommentDto> AddCommentAsync(int postId, CommentAddRequest request, CancellationToken ct);
    Task<CommentDto> UpdateCommentAsync(int commentId, CommentEditRequest request, CancellationToken ct);
    Task DeleteCommentAsync(int commentId, CancellationToken ct);
    Task<CommentDto> SetCommentHiddenAsync(int commentId, bool isHidden, CancellationToken ct);

    Task<PagedResult<ModerationPostDto>> GetModerationPostsAsync(ModerationPostQuery query, CancellationToken ct);
    Task<IReadOnlyList<ModerationCommentDto>> GetModerationCommentsAsync(ModerationCommentQuery query, CancellationToken ct);
}

public sealed class CommunityService(
    IPostRepository posts,
    ICommentRepository comments,
    ICourseRepository courses,
    IEnrollmentRepository enrollments,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : ICommunityService
{
    public const int TitleMax = 120;
    public const int BodyMax = 2000;
    public const int CommentMax = 1000;
    public const string FormerMember = "Former member";
    private const string PostRemovedMessage = "This post was removed.";
    private const string CommentRemovedMessage = "This comment was removed.";

    /// <summary>Courses the user is actively enrolled in, read once per request and user.</summary>
    private (int UserId, HashSet<int> CourseIds)? _activeCourses;

    // ── Reading ─────────────────────────────────────────────────

    public async Task<CommunityInfoDto> GetInfoAsync(string courseSlug, CancellationToken ct)
    {
        var access = await currentUser.TryGetAccessAsync(ct);
        var course = await GetCourseBySlugAsync(access, courseSlug, ct);
        var rights = await RightsAsync(access, course.Id, ct);
        // The teaser count is public (BR-COM-14).
        var count = await posts.CountAsync(p => p.CourseId == course.Id && !p.IsHidden, ct);
        return new CommunityInfoDto(rights.CanRead, rights.CanPost, rights.CanModerate, count);
    }

    public async Task<PagedResult<PostDto>> GetPostsAsync(string courseSlug, PageQuery page, CancellationToken ct)
    {
        var access = await currentUser.GetAccessAsync(ct);
        var course = await GetCourseBySlugAsync(access, courseSlug, ct);
        var rights = await RightsAsync(access, course.Id, ct);
        if (!rights.CanRead) throw new ForbiddenException();

        var list = posts.ListQuery().Where(p => p.CourseId == course.Id && (rights.CanModerate || !p.IsHidden));
        var total = await list.CountAsync(ct);
        // Pinned first, then newest (BR-COM-05).
        var items = await list.OrderByDescending(p => p.IsPinned).ThenByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            .Skip(page.Skip).Take(page.PageSize).ToListAsync(ct);
        return new PagedResult<PostDto>(await ToPostDtosAsync(access, items, ct), total, page.Page, page.PageSize);
    }

    public async Task<IReadOnlyList<CommentDto>> GetCommentsAsync(int postId, CancellationToken ct)
    {
        var access = await currentUser.GetAccessAsync(ct);
        var (post, course, rights) = await GetPostAsync(access, postId, tracking: false, ct);
        if (!rights.CanRead) throw new ForbiddenException();
        var all = await comments.ListForPostsAsync([post.Id], ct);
        var authors = await comments.GetAuthorsAsync(all.Select(c => c.AuthorUserId).Distinct().ToList(), ct);
        return Thread(all, rights.CanModerate)
            .Select(t => ToDto(access, t.Comment, course, rights, authors, t.Replies.Select(r => ToDto(access, r, course, rights, authors, [])).ToList()))
            .ToList();
    }

    // ── Writing ─────────────────────────────────────────────────

    public async Task<PostDto> CreatePostAsync(string courseSlug, PostSaveRequest request, CancellationToken ct)
    {
        var access = await currentUser.GetAccessAsync(ct);
        var course = await GetCourseBySlugAsync(access, courseSlug, ct);
        if (!(await RightsAsync(access, course.Id, ct)).CanPost) throw new ForbiddenException();
        var (title, body) = CheckPost(request);
        var post = new Post { CourseId = course.Id, AuthorUserId = access.UserId, Title = title, Body = body };
        await posts.AddAsync(post, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return (await ToPostDtosAsync(access, [post], ct))[0];
    }

    public async Task<PostDto> UpdatePostAsync(int postId, PostSaveRequest request, CancellationToken ct)
    {
        var access = await currentUser.GetAccessAsync(ct);
        var (post, _, rights) = await GetPostAsync(access, postId, tracking: true, ct);
        // Only the author edits, and only while they can still read the community (BR-COM-08).
        if (post.AuthorUserId != access.UserId || !rights.CanRead) throw new ForbiddenException();
        (post.Title, post.Body) = CheckPost(request);
        post.EditedAt = clock.GetUtcNow().UtcDateTime;
        await unitOfWork.SaveChangesAsync(ct);
        return (await ToPostDtosAsync(access, [post], ct))[0];
    }

    public async Task DeletePostAsync(int postId, CancellationToken ct)
    {
        var access = await currentUser.GetAccessAsync(ct);
        var (post, _, rights) = await GetPostAsync(access, postId, tracking: true, ct);
        if (!CanDelete(access, post.AuthorUserId, rights)) throw new ForbiddenException();
        // Its comments go with it (BR-COM-09).
        foreach (var comment in await comments.ListTrackedForPostAsync(post.Id, ct)) comments.Remove(comment);
        posts.Remove(post);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<PostDto> SetPostFlagsAsync(int postId, PostFlagsRequest request, CancellationToken ct)
    {
        var access = await currentUser.GetAccessAsync(ct);
        var (post, _, rights) = await GetPostAsync(access, postId, tracking: true, ct);
        if (!rights.CanModerate) throw new ForbiddenException();
        if (request.IsPinned is { } pinned) post.IsPinned = pinned;
        if (request.IsLocked is { } locked) post.IsLocked = locked;
        if (request.IsHidden is { } hidden) post.IsHidden = hidden;
        await unitOfWork.SaveChangesAsync(ct);
        return (await ToPostDtosAsync(access, [post], ct))[0];
    }

    public async Task<CommentDto> AddCommentAsync(int postId, CommentAddRequest request, CancellationToken ct)
    {
        var access = await currentUser.GetAccessAsync(ct);
        var (post, course, rights) = await GetPostAsync(access, postId, tracking: false, ct);
        if (!rights.CanPost) throw new ForbiddenException();
        if (post.IsLocked && !rights.CanModerate) throw new ConflictException("This discussion is locked.");
        var body = CheckText(request.Body, CommentMax, "Write a comment.", "body");

        int? parentId = null;
        if (request.ParentId is { } replyTo)
        {
            var parent = (await comments.ListAsync(c => c.Id == replyTo && c.PostId == post.Id, ct)).FirstOrDefault()
                ?? throw new BusinessRuleException("The comment you replied to was removed.");
            // Replies are one level deep: a reply to a reply joins the same thread (BR-COM-07).
            parentId = parent.ParentCommentId ?? parent.Id;
        }

        var comment = new Comment { PostId = post.Id, CourseId = post.CourseId, ParentCommentId = parentId, AuthorUserId = access.UserId, Body = body };
        await comments.AddAsync(comment, ct);
        await unitOfWork.SaveChangesAsync(ct);
        var authors = await comments.GetAuthorsAsync([access.UserId], ct);
        return ToDto(access, comment, course, rights, authors, []);
    }

    public async Task<CommentDto> UpdateCommentAsync(int commentId, CommentEditRequest request, CancellationToken ct)
    {
        var access = await currentUser.GetAccessAsync(ct);
        var (comment, course, rights) = await GetCommentAsync(access, commentId, ct);
        if (comment.AuthorUserId != access.UserId || !rights.CanRead) throw new ForbiddenException();
        comment.Body = CheckText(request.Body, CommentMax, "Write a comment.", "body");
        comment.EditedAt = clock.GetUtcNow().UtcDateTime;
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(access, comment, course, rights, await comments.GetAuthorsAsync([comment.AuthorUserId], ct), []);
    }

    public async Task DeleteCommentAsync(int commentId, CancellationToken ct)
    {
        var access = await currentUser.GetAccessAsync(ct);
        var (comment, _, rights) = await GetCommentAsync(access, commentId, ct);
        if (!CanDelete(access, comment.AuthorUserId, rights)) throw new ForbiddenException();
        // Its replies go with it (BR-COM-09).
        foreach (var reply in await comments.ListRepliesAsync(comment.Id, ct)) comments.Remove(reply);
        comments.Remove(comment);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<CommentDto> SetCommentHiddenAsync(int commentId, bool isHidden, CancellationToken ct)
    {
        var access = await currentUser.GetAccessAsync(ct);
        var (comment, course, rights) = await GetCommentAsync(access, commentId, ct);
        if (!rights.CanModerate) throw new ForbiddenException();
        comment.IsHidden = isHidden;
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(access, comment, course, rights, await comments.GetAuthorsAsync([comment.AuthorUserId], ct), []);
    }

    // ── Moderation lists ────────────────────────────────────────

    public async Task<PagedResult<ModerationPostDto>> GetModerationPostsAsync(ModerationPostQuery query, CancellationToken ct)
    {
        var access = await currentUser.GetAccessAsync(ct);
        var moderated = Moderated(access);
        var list = posts.ListQuery(query.Search?.Trim());
        if (moderated is not null) list = list.Where(p => moderated.Contains(p.CourseId));
        if (query.CourseId is { } courseId) list = list.Where(p => p.CourseId == courseId);
        list = (query.Status?.Trim() ?? "all") switch
        {
            "all" or "" => list,
            "visible" => list.Where(p => !p.IsHidden),
            "hidden" => list.Where(p => p.IsHidden),
            "pinned" => list.Where(p => p.IsPinned),
            "locked" => list.Where(p => p.IsLocked),
            _ => throw new BusinessRuleException("Choose a valid status.", "status"),
        };

        var total = await list.CountAsync(ct);
        var items = await list.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            .Skip(query.Skip).Take(query.PageSize).ToListAsync(ct);
        var dtos = await ToPostDtosAsync(access, items, ct);
        var info = await CourseMapAsync(items.Select(p => p.CourseId), ct);
        return new PagedResult<ModerationPostDto>(
            dtos.Select(p => new ModerationPostDto(p, info[p.CourseId].Title, info[p.CourseId].Slug)).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<IReadOnlyList<ModerationCommentDto>> GetModerationCommentsAsync(ModerationCommentQuery query, CancellationToken ct)
    {
        var access = await currentUser.GetAccessAsync(ct);
        var moderated = Moderated(access);
        var list = comments.ListQuery(query.Search?.Trim());
        if (moderated is not null) list = list.Where(c => moderated.Contains(c.CourseId));
        if (query.CourseId is { } courseId) list = list.Where(c => c.CourseId == courseId);
        list = (query.Status?.Trim() ?? "all") switch
        {
            // Pinned and locked describe posts; they do not filter comments (BR-COM-16).
            "all" or "" or "pinned" or "locked" => list,
            "visible" => list.Where(c => !c.IsHidden),
            "hidden" => list.Where(c => c.IsHidden),
            _ => throw new BusinessRuleException("Choose a valid status.", "status"),
        };

        var items = await list.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id).Take(query.Limit).ToListAsync(ct);
        var postIds = items.Select(c => c.PostId).Distinct().ToList();
        var titles = await posts.ListQuery().Where(p => postIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Title, ct);
        var courseMap = await CourseMapAsync(items.Select(c => c.CourseId), ct);
        var authors = await comments.GetAuthorsAsync(items.Select(c => c.AuthorUserId).Distinct().ToList(), ct);
        var result = new List<ModerationCommentDto>();
        foreach (var c in items.Where(c => titles.ContainsKey(c.PostId) && courseMap.ContainsKey(c.CourseId)))
        {
            var course = courseMap[c.CourseId];
            var rights = await RightsAsync(access, course.Id, ct);
            result.Add(new ModerationCommentDto(ToDto(access, c, course, rights, authors, []), titles[c.PostId], course.Title, course.Slug));
        }
        return result;
    }

    // ── Access ──────────────────────────────────────────────────

    private sealed record Rights(bool CanRead, bool CanPost, bool CanModerate);

    /// <summary>BR-COM-01 to 03: moderators read and post; others need community.view and course access, and community.post to write.</summary>
    private async Task<Rights> RightsAsync(UserAccess? access, int courseId, CancellationToken ct)
    {
        if (access is null) return new Rights(false, false, false);
        var moderate = access.Can(PermissionCodes.CommunityModerate, courseId);
        var read = moderate || (access.Can(PermissionCodes.CommunityView)
            && (access.Can(PermissionCodes.CoursesView, courseId) || (await ActiveCourseIdsAsync(access.UserId, ct)).Contains(courseId)));
        var post = read && (moderate || access.Can(PermissionCodes.CommunityPost));
        return new Rights(read, post, moderate);
    }

    private async Task<HashSet<int>> ActiveCourseIdsAsync(int userId, CancellationToken ct)
    {
        if (_activeCourses is not { } cached || cached.UserId != userId) _activeCourses = (userId, await enrollments.ActiveCourseIdsAsync(userId, ct));
        return _activeCourses.Value.CourseIds;
    }

    private static bool CanDelete(UserAccess access, int authorId, Rights rights) =>
        (authorId == access.UserId && rights.CanRead) || rights.CanModerate;

    /// <summary>Courses whose community the user moderates; null for all. 403 when there are none.</summary>
    private static IReadOnlySet<int>? Moderated(UserAccess access)
    {
        var moderated = access.CoursesWith(PermissionCodes.CommunityModerate);
        if (moderated is { Count: 0 }) throw new ForbiddenException();
        return moderated;
    }

    /// <summary>An unpublished course looks like a missing one to people who may not open it (BR-CAT-02).</summary>
    private static bool CanSee(UserAccess? access, CourseSummary course) =>
        course.IsPublished || access?.Can(PermissionCodes.CoursesView, course.Id) == true;

    private async Task<CourseSummary> GetCourseBySlugAsync(UserAccess? access, string slug, CancellationToken ct)
    {
        var course = (await courses.ListSummariesAsync(c => c.Slug == slug, ct)).FirstOrDefault();
        return course is not null && CanSee(access, course) ? course : throw new NotFoundException("Course not found.");
    }

    /// <summary>The post with its course and the caller's rights. Hidden posts exist only for moderators (BR-COM-05).</summary>
    private async Task<(Post Post, CourseSummary Course, Rights Rights)> GetPostAsync(UserAccess access, int postId, bool tracking, CancellationToken ct)
    {
        var post = tracking
            ? await posts.GetByIdAsync(postId, ct)
            : (await posts.ListAsync(p => p.Id == postId, ct)).FirstOrDefault();
        if (post is null) throw new NotFoundException(PostRemovedMessage);
        var course = (await courses.ListSummariesAsync(c => c.Id == post.CourseId, ct)).FirstOrDefault();
        if (course is null || !CanSee(access, course)) throw new NotFoundException(PostRemovedMessage);
        var rights = await RightsAsync(access, course.Id, ct);
        if (post.IsHidden && !rights.CanModerate) throw new NotFoundException(PostRemovedMessage);
        return (post, course, rights);
    }

    private async Task<(Comment Comment, CourseSummary Course, Rights Rights)> GetCommentAsync(UserAccess access, int commentId, CancellationToken ct)
    {
        var comment = await comments.GetByIdAsync(commentId, ct) ?? throw new NotFoundException(CommentRemovedMessage);
        var course = (await courses.ListSummariesAsync(c => c.Id == comment.CourseId, ct)).FirstOrDefault();
        if (course is null || !CanSee(access, course)) throw new NotFoundException(CommentRemovedMessage);
        return (comment, course, await RightsAsync(access, course.Id, ct));
    }

    // ── Text rules ──────────────────────────────────────────────

    private static (string Title, string Body) CheckPost(PostSaveRequest request) => (
        CheckText(request.Title, TitleMax, "Give your post a title.", "title"),
        CheckText(request.Body, BodyMax, "Write something in your post.", "body"));

    /// <summary>Trimmed, not empty and within the limit (BR-COM-11).</summary>
    private static string CheckText(string? text, int max, string emptyMessage, string field)
    {
        var trimmed = text?.Trim() ?? "";
        if (trimmed.Length == 0) throw new BusinessRuleException(emptyMessage, field);
        if (trimmed.Length > max) throw new BusinessRuleException("This is too long. Please shorten it.", field);
        return trimmed;
    }

    // ── Mapping ─────────────────────────────────────────────────

    private sealed record ThreadItem(Comment Comment, List<Comment> Replies);

    /// <summary>
    /// Top-level comments oldest first with their replies (BR-COM-06, 07). Hidden comments are left out for
    /// non-moderators, and so are replies under a hidden comment.
    /// </summary>
    private static List<ThreadItem> Thread(IReadOnlyCollection<Comment> all, bool moderator)
    {
        var shown = all.Where(c => moderator || !c.IsHidden).OrderBy(c => c.CreatedAt).ThenBy(c => c.Id).ToList();
        var replies = shown.Where(c => c.ParentCommentId is not null).ToLookup(c => c.ParentCommentId!.Value);
        return shown.Where(c => c.ParentCommentId is null).Select(c => new ThreadItem(c, replies[c.Id].ToList())).ToList();
    }

    private async Task<Dictionary<int, CourseSummary>> CourseMapAsync(IEnumerable<int> courseIds, CancellationToken ct)
    {
        var ids = courseIds.Distinct().ToList();
        return (await courses.ListSummariesAsync(c => ids.Contains(c.Id), ct)).ToDictionary(c => c.Id);
    }

    private async Task<List<PostDto>> ToPostDtosAsync(UserAccess access, IReadOnlyCollection<Post> list, CancellationToken ct)
    {
        if (list.Count == 0) return [];
        var courseMap = await CourseMapAsync(list.Select(p => p.CourseId), ct);
        var authors = await comments.GetAuthorsAsync(list.Select(p => p.AuthorUserId).Distinct().ToList(), ct);
        var allComments = (await comments.ListForPostsAsync(list.Select(p => p.Id).ToList(), ct)).ToLookup(c => c.PostId);

        var result = new List<PostDto>();
        foreach (var p in list.Where(p => courseMap.ContainsKey(p.CourseId)))
        {
            var course = courseMap[p.CourseId];
            var rights = await RightsAsync(access, course.Id, ct);
            // The count matches the thread the viewer sees (BR-COM-06).
            var count = Thread(allComments[p.Id].ToList(), rights.CanModerate).Sum(t => 1 + t.Replies.Count);
            result.Add(new PostDto(
                p.Id, p.CourseId, p.Title, p.Body, Author(p.AuthorUserId, course, authors), p.CreatedAt, p.EditedAt,
                p.IsPinned, p.IsLocked, p.IsHidden, count,
                p.AuthorUserId == access.UserId && rights.CanRead, CanDelete(access, p.AuthorUserId, rights),
                rights.CanPost && (!p.IsLocked || rights.CanModerate)));
        }
        return result;
    }

    private static CommentDto ToDto(
        UserAccess access, Comment c, CourseSummary course, Rights rights, IReadOnlyDictionary<int, CommunityAuthor> authors, IReadOnlyList<CommentDto> replies) =>
        new(c.Id, c.PostId, c.CourseId, c.ParentCommentId, c.Body, Author(c.AuthorUserId, course, authors), c.CreatedAt, c.EditedAt, c.IsHidden,
            c.AuthorUserId == access.UserId && rights.CanRead, CanDelete(access, c.AuthorUserId, rights), replies);

    /// <summary>
    /// Badge: admin for the admin role; instructor when the user teaches the course or may edit it through a grant;
    /// otherwise student (BR-COM-12). Deleted accounts show as "Former member" (BR-COM-13).
    /// </summary>
    private static AuthorDto Author(int userId, CourseSummary course, IReadOnlyDictionary<int, CommunityAuthor> authors)
    {
        if (!authors.TryGetValue(userId, out var a)) return new AuthorDto(null, FormerMember, "student");
        var badge = a.IsAdmin ? "admin"
            : a.InstructorId == course.InstructorId || a.EditGrantCourseIds.Contains(course.Id) ? "instructor"
            : "student";
        return new AuthorDto(a.Id, a.Name, badge);
    }
}
