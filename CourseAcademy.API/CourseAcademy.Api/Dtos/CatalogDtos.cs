namespace CourseAcademy.Api.Dtos;

// ── Read models ─────────────────────────────────────────────────

public sealed record InstructorDto(
    int Id, string Name, string Title, string Bio, IReadOnlyList<string> Expertise,
    int ExperienceYears, int Students, decimal Rating);

public static class MediaSources
{
    public const string External = "external";
    public const string Upload = "upload";
}

/// <summary>
/// Url is null when the caller may not open it (IsLocked) or when the media is an upload (SourceType "upload"),
/// which is opened through the media endpoints. HasMedia is false when no link or ready file is there yet; such
/// items are only returned to people who manage the course, who also get the upload's details in Media.
/// </summary>
public sealed record VideoDto(int Id, string Title, int Minutes, bool IsPreview, string? SourceType, string? Url, bool HasMedia, bool IsLocked, MediaAssetDto? Media = null);

public sealed record DocumentDto(int Id, string Title, string TypeCode, int? VideoId, string? SourceType, string? Url, bool HasMedia, bool IsLocked, MediaAssetDto? Media = null);

public sealed record SectionDto(
    int Id, string Title, int Depth, IReadOnlyList<VideoDto> Videos, IReadOnlyList<DocumentDto> Documents, IReadOnlyList<SectionDto> Topics);

public sealed record SubmoduleDto(int Id, string Title, IReadOnlyList<SectionDto> Sections);

public sealed record ModuleDto(int Id, string Title, IReadOnlyList<SubmoduleDto> Submodules);

/// <summary>StartsAt is Bangladesh local time, "YYYY-MM-DDTHH:mm". MeetingUrl is null without course access.</summary>
public sealed record LiveSessionDto(int Id, string Title, string StartsAt, int DurationMinutes, string? MeetingUrl);

public sealed record MaterialDto(int Id, string Title, string TypeCode, string? SourceType, string? Url, bool HasMedia, bool IsLocked, MediaAssetDto? Media = null);

/// <summary>A course as a catalogue card.</summary>
public sealed record CourseCardDto(
    int Id, string Slug, string Title, string Subtitle, string CategoryCode, string FormatCode, string LevelCode, string? BadgeCode,
    bool IsPublished, bool IsFeatured, int InstructorId, string InstructorName,
    decimal Price, decimal OriginalPrice, int DiscountPercent, decimal Rating, int RatingCount, int Students, int DurationHours,
    string Language, string CoverLabel, string CoverIcon, string? ImageUrl, string ContentUpdatedMonth, int SectionCount);

/// <summary>
/// A whole course. HasContentAccess tells whether the caller may open locked lessons and links.
/// The enrollment counts are only filled for people who manage the course.
/// </summary>
public sealed record CourseDetailDto(
    int Id, string Slug, string Title, string Subtitle, string Description, string CategoryCode, string FormatCode, string LevelCode,
    string? BadgeCode, bool IsPublished, bool IsFeatured, int InstructorId, string Language, int DurationHours,
    decimal Price, decimal OriginalPrice, int DiscountPercent, string CoverLabel, string CoverIcon, string? ImageUrl,
    decimal Rating, int RatingCount, int Students, string ContentUpdatedMonth, int SectionCount,
    IReadOnlyList<string> Outcomes, IReadOnlyList<string> Requirements,
    IReadOnlyList<ModuleDto> Modules, IReadOnlyList<LiveSessionDto> Schedule, IReadOnlyList<MaterialDto> Materials,
    bool HasContentAccess, int? ActiveEnrollmentCount = null, int? EnrollmentCount = null);

public sealed class CourseSearchQuery : Common.PageQuery
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    /// <summary>all, under-2500, 2500-4500, over-4500</summary>
    public string? Price { get; set; }
    /// <summary>Comma separated level codes.</summary>
    public string? Levels { get; set; }
    /// <summary>Comma separated format codes.</summary>
    public string? Formats { get; set; }
    public bool? Featured { get; set; }
    /// <summary>popular (default), rating, newest, price-asc, price-desc</summary>
    public string? Sort { get; set; }
}

// ── Write models ────────────────────────────────────────────────

/// <summary>StartsAt is Bangladesh local time, "YYYY-MM-DDTHH:mm". Rows without Id are created.</summary>
public sealed record LiveSessionInput(int? Id, string Title, string StartsAt, int DurationMinutes, string MeetingUrl);

/// <summary>
/// Link materials need Url. File materials hold an uploaded document (MediaAssetId of this course), an older link,
/// or nothing yet. The request is the full state: what it leaves out is cleared.
/// </summary>
public sealed record MaterialInput(int? Id, string Title, string TypeCode, string? Url, int? MediaAssetId = null);

/// <summary>Course details. Lists replace the stored lists; their order is the display order.</summary>
public sealed record CourseUpsertRequest(
    string Title, string Slug, string Subtitle, string Description, string CategoryCode, string FormatCode, string LevelCode,
    string? BadgeCode, int InstructorId, string Language, int DurationHours, decimal OriginalPrice, decimal Price,
    string CoverLabel, string CoverIcon, string? ImageUrl, bool IsFeatured, bool IsPublished,
    IReadOnlyList<string> Outcomes, IReadOnlyList<string>? Requirements,
    IReadOnlyList<LiveSessionInput>? Schedule, IReadOnlyList<MaterialInput>? Materials);

public sealed record SetPublishedRequest(bool IsPublished);

/// <summary>
/// Id: an existing video of this course, or null for a new one. TempKey lets a new document point to a new video.
/// MediaAssetId: an uploaded video of this course; required for new videos, and replaces the media of an existing one.
/// Url: only the existing link of an older video (links can no longer be added or changed). Both null keep the stored media.
/// </summary>
public sealed record VideoInput(int? Id, string? TempKey, string Title, string? Url, int Minutes, bool IsPreview, int? MediaAssetId = null);

/// <summary>
/// VideoId or VideoTempKey attach the document to a video of the same section; both null = whole section.
/// The file is an upload (MediaAssetId), a link (Url) or nothing yet; the request is the full state.
/// </summary>
public sealed record DocumentInput(int? Id, string Title, string TypeCode, string? Url, int? VideoId, string? VideoTempKey, int? MediaAssetId = null);

public sealed record SectionInput(int? Id, string Title, IReadOnlyList<VideoInput>? Videos, IReadOnlyList<DocumentInput>? Documents, IReadOnlyList<SectionInput>? Topics);

public sealed record SubmoduleInput(int? Id, string Title, IReadOnlyList<SectionInput> Sections);

public sealed record ModuleInput(int? Id, string Title, IReadOnlyList<SubmoduleInput> Submodules);

public sealed record CurriculumRequest(IReadOnlyList<ModuleInput> Modules);

public sealed record SlugAvailabilityDto(bool Available);

public sealed record InstructorSaveRequest(
    string Name, string Title, string Bio, IReadOnlyList<string>? Expertise, int ExperienceYears, int Students);
