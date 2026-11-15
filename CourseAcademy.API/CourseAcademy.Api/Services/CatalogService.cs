using CourseAcademy.Api.Common;
using CourseAcademy.Api.Common.Auth;
using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Api.Services;

/// <summary>Public reading of courses and instructors. Drafts are visible only to people who manage the course.</summary>
public interface ICatalogService
{
    /// <summary>Every course the caller may see, in full. Used by the frontend, which keeps the catalogue in memory.</summary>
    Task<IReadOnlyList<CourseDetailDto>> GetAllAsync(CancellationToken ct);

    Task<CourseDetailDto> GetBySlugAsync(string slug, CancellationToken ct);

    /// <summary>Published courses, filtered, sorted and paged (BR-CAT-01, 03–06).</summary>
    Task<PagedResult<CourseCardDto>> SearchAsync(CourseSearchQuery query, CancellationToken ct);

    /// <summary>Other published courses, same category first (BR-CAT-11).</summary>
    Task<IReadOnlyList<CourseCardDto>> GetRelatedAsync(string slug, int limit, CancellationToken ct);

    Task<IReadOnlyList<InstructorDto>> GetInstructorsAsync(CancellationToken ct);
}

public sealed class CatalogService(ICourseRepository courses, IInstructorRepository instructors, ICurrentUser currentUser) : ICatalogService
{
    public async Task<IReadOnlyList<CourseDetailDto>> GetAllAsync(CancellationToken ct)
    {
        var access = await currentUser.TryGetAccessAsync(ct);
        var list = await courses.ListWithDetailsAsync(c => true, tracking: false, ct);
        var visible = list.Where(c => c.IsPublished || CanManage(access, c.Id)).ToList();
        var sections = (await courses.ListSectionsAsync(visible.Select(c => c.Id).ToList(), tracking: false, ct)).ToLookup(s => s.CourseId);
        return visible.Select(c => CourseMapper.ToDetail(c, sections[c.Id].ToList(), Viewer(access, c.Id))).ToList();
    }

    public async Task<CourseDetailDto> GetBySlugAsync(string slug, CancellationToken ct)
    {
        var access = await currentUser.TryGetAccessAsync(ct);
        var course = (await courses.ListWithDetailsAsync(c => c.Slug == slug, tracking: false, ct)).FirstOrDefault();
        // An unpublished course looks like a missing one to everybody who may not manage it (BR-CAT-02).
        if (course is null || (!course.IsPublished && !CanManage(access, course.Id))) throw new NotFoundException("Course not found.");
        var sections = await courses.ListSectionsAsync([course.Id], tracking: false, ct);
        return CourseMapper.ToDetail(course, sections, Viewer(access, course.Id));
    }

    public async Task<PagedResult<CourseCardDto>> SearchAsync(CourseSearchQuery q, CancellationToken ct)
    {
        var query = courses.CatalogQuery().Where(c => c.IsPublished);

        var term = q.Search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(c => c.Title.Contains(term) || c.Subtitle.Contains(term) || c.CoverLabel.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(q.Category) && q.Category != "all") query = query.Where(c => c.Category.Code == q.Category);
        query = q.Price switch
        {
            "under-2500" => query.Where(c => c.Price < 2500),
            "2500-4500" => query.Where(c => c.Price >= 2500 && c.Price <= 4500),
            "over-4500" => query.Where(c => c.Price > 4500),
            _ => query,
        };
        var levels = SplitCodes(q.Levels);
        if (levels.Count > 0) query = query.Where(c => levels.Contains(c.Level.Code));
        var formats = SplitCodes(q.Formats);
        if (formats.Count > 0) query = query.Where(c => formats.Contains(c.Format.Code));
        if (q.Featured is { } featured) query = query.Where(c => c.IsFeatured == featured);

        query = q.Sort switch
        {
            "rating" => query.OrderByDescending(c => c.Rating).ThenByDescending(c => c.RatingCount).ThenBy(c => c.Id),
            "price-asc" => query.OrderBy(c => c.Price).ThenBy(c => c.Id),
            "price-desc" => query.OrderByDescending(c => c.Price).ThenBy(c => c.Id),
            "newest" => query.OrderByDescending(c => c.ContentUpdatedMonth).ThenBy(c => c.Id),
            _ => query.OrderByDescending(c => c.Students).ThenBy(c => c.Id),
        };

        var total = await query.CountAsync(ct);
        var page = await query.Skip(q.Skip).Take(q.PageSize).ToListAsync(ct);
        var counts = await courses.CountSectionsAsync(page.Select(c => c.Id).ToList(), ct);
        return new PagedResult<CourseCardDto>(page.Select(c => CourseMapper.ToCard(c, counts.GetValueOrDefault(c.Id))).ToList(), total, q.Page, q.PageSize);
    }

    public async Task<IReadOnlyList<CourseCardDto>> GetRelatedAsync(string slug, int limit, CancellationToken ct)
    {
        var all = await courses.CatalogQuery().Where(c => c.IsPublished || c.Slug == slug).OrderBy(c => c.Id).ToListAsync(ct);
        var course = all.FirstOrDefault(c => c.Slug == slug);
        if (course is null) throw new NotFoundException("Course not found.");
        var others = all.Where(c => c.Id != course.Id && c.IsPublished).ToList();
        var related = others.Where(c => c.CategoryId == course.CategoryId)
            .Concat(others.Where(c => c.CategoryId != course.CategoryId))
            .Take(Math.Clamp(limit, 1, 12))
            .ToList();
        var counts = await courses.CountSectionsAsync(related.Select(c => c.Id).ToList(), ct);
        return related.Select(c => CourseMapper.ToCard(c, counts.GetValueOrDefault(c.Id))).ToList();
    }

    public async Task<IReadOnlyList<InstructorDto>> GetInstructorsAsync(CancellationToken ct) =>
        (await instructors.ListWithExpertiseAsync(ct)).Select(CourseMapper.ToDto).ToList();

    private static bool CanManage(UserAccess? access, int courseId) => access?.Can(PermissionCodes.CoursesView, courseId) == true;

    /// <summary>Course access: managing the course. Enrolled students get access when enrollments move to the API.</summary>
    private static CourseViewer Viewer(UserAccess? access, int courseId)
    {
        var manage = CanManage(access, courseId);
        return new CourseViewer(manage, manage);
    }

    private static List<string> SplitCodes(string? csv) =>
        string.IsNullOrWhiteSpace(csv) ? [] : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
