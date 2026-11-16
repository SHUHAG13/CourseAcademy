namespace CourseAcademy.Api.Entities;

/// <summary>A fixed list of values (statuses, types). Code is what the API and the frontend use; it never changes.</summary>
public abstract class LookupEntity : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public sealed class CourseFormat : LookupEntity;

public sealed class CourseLevel : LookupEntity;

public sealed class CourseBadge : LookupEntity;

public sealed class DocumentType : LookupEntity
{
    /// <summary>Extension a document of this type must have, e.g. ".pdf".</summary>
    public string FileExtension { get; set; } = string.Empty;
}

public sealed class MaterialType : LookupEntity;

public sealed class EnrollmentStatus : LookupEntity;

public sealed class PaymentStatus : LookupEntity;

public sealed class Currency : LookupEntity;

public sealed class MediaKind : LookupEntity;

public sealed class MediaAssetStatus : LookupEntity;

public sealed class Category : LookupEntity
{
    public string ShortName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public ICollection<CategorySkill> Skills { get; set; } = [];
}

public sealed class PaymentMethod : LookupEntity
{
    public string Description { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string Icon { get; set; } = string.Empty;
}
