using CourseAcademy.Api.Common.Auth;
using CourseAcademy.Api.Data;
using CourseAcademy.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Api.Repositories;

/// <summary>
/// An author as the community shows them. EditGrantCourseIds: courses where the user has <c>courses.edit</c> through a
/// course grant, which makes them an instructor there (BR-COM-12).
/// </summary>
public sealed record CommunityAuthor(int Id, string Name, bool IsAdmin, int? InstructorId, IReadOnlySet<int> EditGrantCourseIds);

public interface IPostRepository : IRepository<Post>
{
    /// <summary>Read-only posts; <paramref name="search"/> matches the title, the text or the author's name.</summary>
    IQueryable<Post> ListQuery(string? search = null);

    /// <summary>Tracked posts of one course, for deleting them with the course.</summary>
    Task<List<Post>> ListForCourseAsync(int courseId, CancellationToken ct);
}

public sealed class PostRepository(AppDbContext db) : Repository<Post>(db), IPostRepository
{
    public IQueryable<Post> ListQuery(string? search = null) =>
        string.IsNullOrWhiteSpace(search)
            ? Query
            : Query.Where(p => p.Title.Contains(search) || p.Body.Contains(search)
                || Db.Users.Any(u => u.Id == p.AuthorUserId && u.Name.Contains(search)));

    public Task<List<Post>> ListForCourseAsync(int courseId, CancellationToken ct) => Set.Where(p => p.CourseId == courseId).ToListAsync(ct);
}

public interface ICommentRepository : IRepository<Comment>
{
    /// <summary>Read-only comments; <paramref name="search"/> matches the text or the author's name.</summary>
    IQueryable<Comment> ListQuery(string? search = null);

    /// <summary>Read-only comments and replies of the given posts, oldest first.</summary>
    Task<List<Comment>> ListForPostsAsync(IReadOnlyCollection<int> postIds, CancellationToken ct);

    /// <summary>Tracked comments of one post, for deleting them with the post.</summary>
    Task<List<Comment>> ListTrackedForPostAsync(int postId, CancellationToken ct);

    /// <summary>Tracked replies to one comment.</summary>
    Task<List<Comment>> ListRepliesAsync(int commentId, CancellationToken ct);

    /// <summary>Tracked comments of one course, for deleting them with the course.</summary>
    Task<List<Comment>> ListForCourseAsync(int courseId, CancellationToken ct);

    /// <summary>Names, roles and edit grants of the given users. Deleted accounts are missing (BR-COM-13).</summary>
    Task<Dictionary<int, CommunityAuthor>> GetAuthorsAsync(IReadOnlyCollection<int> userIds, CancellationToken ct);
}

public sealed class CommentRepository(AppDbContext db) : Repository<Comment>(db), ICommentRepository
{
    public IQueryable<Comment> ListQuery(string? search = null) =>
        string.IsNullOrWhiteSpace(search)
            ? Query
            : Query.Where(c => c.Body.Contains(search) || Db.Users.Any(u => u.Id == c.AuthorUserId && u.Name.Contains(search)));

    public Task<List<Comment>> ListForPostsAsync(IReadOnlyCollection<int> postIds, CancellationToken ct) =>
        Query.Where(c => postIds.Contains(c.PostId)).OrderBy(c => c.CreatedAt).ThenBy(c => c.Id).ToListAsync(ct);

    public Task<List<Comment>> ListTrackedForPostAsync(int postId, CancellationToken ct) => Set.Where(c => c.PostId == postId).ToListAsync(ct);

    public Task<List<Comment>> ListRepliesAsync(int commentId, CancellationToken ct) => Set.Where(c => c.ParentCommentId == commentId).ToListAsync(ct);

    public Task<List<Comment>> ListForCourseAsync(int courseId, CancellationToken ct) => Set.Where(c => c.CourseId == courseId).ToListAsync(ct);

    public async Task<Dictionary<int, CommunityAuthor>> GetAuthorsAsync(IReadOnlyCollection<int> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0) return [];
        var users = await Db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Name, RoleCode = u.Role.Code, u.InstructorId })
            .ToListAsync(ct);
        var editGrants = (await Db.CourseGrants.AsNoTracking()
                .Where(g => userIds.Contains(g.UserId) && g.Permissions.Any(p => p.Permission.Code == PermissionCodes.CoursesEdit))
                .Select(g => new { g.UserId, g.CourseId })
                .ToListAsync(ct))
            .ToLookup(g => g.UserId, g => g.CourseId);
        return users.ToDictionary(u => u.Id, u => new CommunityAuthor(
            u.Id, u.Name, u.RoleCode == Role.AdminCode, u.InstructorId, editGrants[u.Id].ToHashSet()));
    }
}
