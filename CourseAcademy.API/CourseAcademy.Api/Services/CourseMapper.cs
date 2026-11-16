using CourseAcademy.Api.Common;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Services.Media;

namespace CourseAcademy.Api.Services;

/// <summary>What one caller may see of one course.</summary>
/// <param name="CanManage">Has <c>courses.view</c> on the course: sees drafts, items without media, and every link.</param>
/// <param name="HasContentAccess">May open locked lessons, documents, materials and meeting links.</param>
public readonly record struct CourseViewer(bool CanManage, bool HasContentAccess);

/// <summary>Builds course DTOs and applies what a caller may see (BR-CAT-13 to 15, media rule B).</summary>
public static class CourseMapper
{
    public static int DiscountPercent(decimal price, decimal originalPrice) =>
        originalPrice > 0 ? (int)Math.Round((1 - price / originalPrice) * 100, MidpointRounding.AwayFromZero) : 0;

    public static InstructorDto ToDto(Instructor i) => new(
        i.Id, i.Name, i.Title, i.Bio, i.Expertise.OrderBy(e => e.SortOrder).Select(e => e.Expertise).ToList(),
        i.ExperienceYears, i.Students, i.Rating);

    public static CourseCardDto ToCard(Course c, int sectionCount) => new(
        c.Id, c.Slug, c.Title, c.Subtitle, c.Category.Code, c.Format.Code, c.Level.Code, c.Badge?.Code,
        c.IsPublished, c.IsFeatured, c.InstructorId, c.Instructor.Name,
        c.Price, c.OriginalPrice, DiscountPercent(c.Price, c.OriginalPrice), c.Rating, c.RatingCount, c.Students, c.DurationHours,
        c.Language, c.CoverLabel, c.CoverIcon, c.ImageUrl, c.ContentUpdatedMonth, sectionCount);

    public static CourseDetailDto ToDetail(Course c, IReadOnlyList<Section> sections, CourseViewer viewer, (int Active, int Total)? enrollments = null)
    {
        var bySubmodule = sections.Where(s => s.SubmoduleId is not null).ToLookup(s => s.SubmoduleId!.Value);
        var byParent = sections.Where(s => s.ParentSectionId is not null).ToLookup(s => s.ParentSectionId!.Value);

        SectionDto Map(Section s)
        {
            var videos = s.Videos.OrderBy(v => v.SortOrder).ThenBy(v => v.Id)
                .Where(v => viewer.CanManage || HasMedia(v))
                .ToList();
            var previewVideoIds = videos.Where(v => v.IsPreview).Select(v => v.Id).ToHashSet();
            var visibleVideoIds = videos.Select(v => v.Id).ToHashSet();

            return new SectionDto(
                s.Id, s.Title, s.Depth,
                videos.Select(v =>
                {
                    var locked = !(v.IsPreview || viewer.HasContentAccess);
                    return new VideoDto(v.Id, v.Title, v.Minutes, v.IsPreview, SourceOf(v), locked ? null : LinkOf(v), HasMedia(v), locked, MediaOf(v, viewer));
                }).ToList(),
                s.Documents.OrderBy(d => d.SortOrder).ThenBy(d => d.Id)
                    .Where(d => viewer.CanManage || HasMedia(d))
                    .Select(d =>
                    {
                        // A document of a free preview video is open to everyone (BR-CAT-13).
                        var locked = !(viewer.HasContentAccess || (d.SectionVideoId is { } vid && previewVideoIds.Contains(vid)));
                        var videoId = d.SectionVideoId is { } id && visibleVideoIds.Contains(id) ? id : (int?)null;
                        return new DocumentDto(d.Id, d.Title, d.DocumentType.Code, videoId, SourceOf(d), locked ? null : LinkOf(d), HasMedia(d), locked, MediaOf(d, viewer));
                    }).ToList(),
                byParent[s.Id].OrderBy(t => t.SortOrder).ThenBy(t => t.Id).Select(Map).ToList());
        }

        var modules = c.Modules.OrderBy(m => m.SortOrder).ThenBy(m => m.Id).Select(m => new ModuleDto(
            m.Id, m.Title,
            m.Submodules.OrderBy(sm => sm.SortOrder).ThenBy(sm => sm.Id).Select(sm => new SubmoduleDto(
                sm.Id, sm.Title,
                bySubmodule[sm.Id].OrderBy(s => s.SortOrder).ThenBy(s => s.Id).Select(Map).ToList())).ToList())).ToList();

        var materials = c.Materials.OrderBy(m => m.SortOrder).ThenBy(m => m.Id)
            .Where(m => viewer.CanManage || HasMedia(m))
            .Select(m => new MaterialDto(m.Id, m.Title, m.MaterialType.Code, SourceOf(m),
                viewer.HasContentAccess ? LinkOf(m) : null, HasMedia(m), !viewer.HasContentAccess, MediaOf(m, viewer)))
            .ToList();

        return new CourseDetailDto(
            c.Id, c.Slug, c.Title, c.Subtitle, c.Description, c.Category.Code, c.Format.Code, c.Level.Code, c.Badge?.Code,
            c.IsPublished, c.IsFeatured, c.InstructorId, c.Language, c.DurationHours,
            c.Price, c.OriginalPrice, DiscountPercent(c.Price, c.OriginalPrice), c.CoverLabel, c.CoverIcon, c.ImageUrl,
            c.Rating, c.RatingCount, c.Students, c.ContentUpdatedMonth, sections.Count,
            c.Outcomes.OrderBy(o => o.SortOrder).Select(o => o.Text).ToList(),
            c.Requirements.OrderBy(r => r.SortOrder).Select(r => r.Text).ToList(),
            modules,
            c.Sessions.OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
                .Select(s => new LiveSessionDto(s.Id, s.Title, BangladeshTime.FromUtc(s.StartsAtUtc), s.DurationMinutes,
                    viewer.HasContentAccess ? s.MeetingUrl : null))
                .ToList(),
            materials,
            viewer.HasContentAccess,
            viewer.CanManage ? enrollments?.Active ?? 0 : null,
            viewer.CanManage ? enrollments?.Total ?? 0 : null);
    }

    /// <summary>
    /// Whether students can open the item: it has a link, or an uploaded file that is ready (media rule B: files
    /// still processing, or failed, are hidden). Needs <see cref="IMediaItem.Asset"/> loaded.
    /// </summary>
    public static bool HasMedia(IMediaItem item) =>
        item.MediaAssetId is null ? item.ExternalUrl is not null : item.Asset?.MediaAssetStatusId == MediaStatusCodes.Id(MediaStatusCodes.Ready);

    private static string? SourceOf(IMediaItem item) =>
        item.MediaAssetId is not null ? MediaSources.Upload : item.ExternalUrl is not null ? MediaSources.External : null;

    /// <summary>Uploads are never sent as URLs; they are opened through the media endpoints.</summary>
    private static string? LinkOf(IMediaItem item) => item.MediaAssetId is null ? item.ExternalUrl : null;

    private static MediaAssetDto? MediaOf(IMediaItem item, CourseViewer viewer) =>
        viewer.CanManage && item.Asset is not null ? MediaService.ToDto(item.Asset) : null;
}
