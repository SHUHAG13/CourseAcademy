namespace CourseAcademy.Api.Entities;

public sealed class CategorySkill : BaseEntity, IHardDelete
{
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public string Skill { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

/// <summary>A public instructor profile. A user account can be linked to it through <see cref="User.InstructorId"/>.</summary>
public sealed class Instructor : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    /// <summary>Entered by hand; not counted from enrollments.</summary>
    public int Students { get; set; }
    public decimal Rating { get; set; }
    public ICollection<InstructorExpertise> Expertise { get; set; } = [];
}

public sealed class InstructorExpertise : BaseEntity, IHardDelete
{
    public int InstructorId { get; set; }
    public Instructor Instructor { get; set; } = null!;
    public string Expertise { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public sealed class Course : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int CourseFormatId { get; set; }
    public CourseFormat Format { get; set; } = null!;
    public int CourseLevelId { get; set; }
    public CourseLevel Level { get; set; } = null!;
    public int? CourseBadgeId { get; set; }
    public CourseBadge? Badge { get; set; }
    public int InstructorId { get; set; }
    public Instructor Instructor { get; set; } = null!;

    /// <summary>Hidden from the public catalogue until true.</summary>
    public bool IsPublished { get; set; }
    public bool IsFeatured { get; set; }
    public string Language { get; set; } = string.Empty;
    public int DurationHours { get; set; }
    /// <summary>Regular price, shown struck through.</summary>
    public decimal OriginalPrice { get; set; }
    /// <summary>Price charged. 0 means free.</summary>
    public decimal Price { get; set; }
    public string CoverLabel { get; set; } = string.Empty;
    public string CoverIcon { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    // Demo statistics; not edited by the course form and not calculated yet.
    public decimal Rating { get; set; }
    public int RatingCount { get; set; }
    public int Students { get; set; }

    /// <summary>"YYYY-MM" of the last save, shown as "Updated".</summary>
    public string ContentUpdatedMonth { get; set; } = string.Empty;

    public ICollection<CourseOutcome> Outcomes { get; set; } = [];
    public ICollection<CourseRequirement> Requirements { get; set; } = [];
    public ICollection<CourseModule> Modules { get; set; } = [];
    public ICollection<LiveSession> Sessions { get; set; } = [];
    public ICollection<Material> Materials { get; set; } = [];
}

public sealed class CourseOutcome : BaseEntity, IHardDelete
{
    public int CourseId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public sealed class CourseRequirement : BaseEntity, IHardDelete
{
    public int CourseId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public sealed class CourseModule : BaseEntity
{
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public ICollection<Submodule> Submodules { get; set; } = [];
}

public sealed class Submodule : BaseEntity
{
    public int CourseModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    /// <summary>Top-level sections only (2–3). Topics hang below them through <see cref="Section.ParentSectionId"/>.</summary>
    public ICollection<Section> Sections { get; set; } = [];
}

/// <summary>A section (Depth 0, under a submodule) or a topic (Depth 1–3, under another section).</summary>
public sealed class Section : BaseEntity
{
    public const int MaxTopicDepth = 3;

    public int CourseId { get; set; }
    public int? SubmoduleId { get; set; }
    public int? ParentSectionId { get; set; }
    public Section? ParentSection { get; set; }
    public int Depth { get; set; }
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public ICollection<Section> Topics { get; set; } = [];
    public ICollection<SectionVideo> Videos { get; set; } = [];
    public ICollection<SectionDocument> Documents { get; set; } = [];
}

/// <summary>
/// Media is either an external link (seeded videos, and new items until uploads exist) or an uploaded
/// Streaming asset. The Main App never stores a URL for uploaded media, only the asset id.
/// </summary>
public interface IMediaItem
{
    string? ExternalUrl { get; }
    string? AssetId { get; }
}

public sealed class SectionVideo : BaseEntity, IMediaItem
{
    public int SectionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ExternalUrl { get; set; }
    public string? AssetId { get; set; }
    public int Minutes { get; set; }
    /// <summary>Visitors who are not enrolled can watch it and open its documents.</summary>
    public bool IsPreview { get; set; }
    public int SortOrder { get; set; }
}

public sealed class SectionDocument : BaseEntity, IMediaItem
{
    public int SectionId { get; set; }
    /// <summary>The video this document belongs to, or null for the whole section.</summary>
    public int? SectionVideoId { get; set; }
    public SectionVideo? Video { get; set; }
    public int DocumentTypeId { get; set; }
    public DocumentType DocumentType { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? ExternalUrl { get; set; }
    public string? AssetId { get; set; }
    public int SortOrder { get; set; }
}

public sealed class LiveSession : BaseEntity
{
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartsAtUtc { get; set; }
    public int DurationMinutes { get; set; }
    public string MeetingUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public sealed class Material : BaseEntity, IMediaItem
{
    public int CourseId { get; set; }
    public int MaterialTypeId { get; set; }
    public MaterialType MaterialType { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    /// <summary>Required for Link materials; a temporary link for files until uploads exist.</summary>
    public string? ExternalUrl { get; set; }
    public string? AssetId { get; set; }
    public int SortOrder { get; set; }
}
